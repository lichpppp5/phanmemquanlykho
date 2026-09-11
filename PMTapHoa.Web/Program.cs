using Microsoft.AspNetCore.Mvc;
using PMTapHoa.Core;
using PMTapHoa.Core.Models;
using PMTapHoa.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5050");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var appDataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PMTapHoa");
Directory.CreateDirectory(appDataDir);

var sqlPath = Path.Combine(AppContext.BaseDirectory, "database.sql");
if (!File.Exists(sqlPath))
{
    var altPath = Path.Combine(Directory.GetCurrentDirectory(), "database.sql");
    if (File.Exists(altPath)) sqlPath = altPath;
    else
    {
        var corePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PMTapHoa.Core", "database.sql");
        if (File.Exists(corePath)) sqlPath = corePath;
    }
}

var coreServices = new CoreServices(appDataDir, sqlPath);
builder.Services.AddSingleton(coreServices);

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// ==================== AUTH / RBAC HELPERS ====================
static bool IsAdmin(HttpContext ctx, CoreServices services)
{
    var roleHeader = ctx.Request.Headers["X-User-Role"].FirstOrDefault();
    if (!string.IsNullOrEmpty(roleHeader))
    {
        return string.Equals(roleHeader, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    var username = ctx.Request.Headers["X-Username"].FirstOrDefault()
                   ?? ctx.Request.Query["operatorUser"].FirstOrDefault();
    if (!string.IsNullOrEmpty(username))
    {
        var user = services.UserService.GetAll().FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user != null)
        {
            return string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
    }

    return true;
}

static IResult ForbiddenResult() =>
    Results.Json(new { message = "Chỉ Quản trị viên (Admin) mới có quyền thực hiện chức năng này." }, statusCode: StatusCodes.Status403Forbidden);

// ==================== API: SẢN PHẨM & DANH MỤC ====================
app.MapGet("/api/products", (HttpContext ctx, [FromServices] CoreServices services, [FromQuery] string? search, [FromQuery] int? categoryId) =>
{
    List<Product> list;
    if (!string.IsNullOrWhiteSpace(search))
    {
        list = services.ProductService.Search(search.Trim());
    }
    else if (categoryId.HasValue)
    {
        list = services.ProductService.GetByCategory(categoryId.Value);
    }
    else
    {
        list = services.ProductService.GetAll();
    }

    if (!IsAdmin(ctx, services))
    {
        // Mask cost price for staff
        list = list.Select(p => new Product
        {
            ProductID = p.ProductID,
            Barcode = p.Barcode,
            ProductName = p.ProductName,
            CategoryID = p.CategoryID,
            CategoryName = p.CategoryName,
            Unit = p.Unit,
            CostPrice = 0,
            SellingPrice = p.SellingPrice,
            StockQuantity = p.StockQuantity,
            MinStock = p.MinStock,
            ExpiryDate = p.ExpiryDate,
            SupplierID = p.SupplierID,
            ImageUrl = p.ImageUrl
        }).ToList();
    }

    return Results.Ok(list);
});

app.MapGet("/api/products/barcode/{barcode}", ([FromServices] CoreServices services, string barcode) =>
{
    var product = services.ProductService.GetByBarcode(barcode.Trim());
    return product != null ? Results.Ok(product) : Results.NotFound(new { message = "Không tìm thấy sản phẩm có mã vạch này" });
});

app.MapGet("/api/products/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id) =>
{
    var p = services.ProductService.GetAll().FirstOrDefault(x => x.ProductID == id);
    if (p == null) return Results.NotFound();
    if (!IsAdmin(ctx, services))
    {
        p = new Product
        {
            ProductID = p.ProductID,
            Barcode = p.Barcode,
            ProductName = p.ProductName,
            CategoryID = p.CategoryID,
            CategoryName = p.CategoryName,
            Unit = p.Unit,
            CostPrice = 0,
            SellingPrice = p.SellingPrice,
            StockQuantity = p.StockQuantity,
            MinStock = p.MinStock,
            ExpiryDate = p.ExpiryDate,
            SupplierID = p.SupplierID,
            ImageUrl = p.ImageUrl
        };
    }
    return Results.Ok(p);
});

app.MapPost("/api/upload/image", async (HttpContext ctx, IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "Vui lòng chọn file ảnh hợp lệ." });
    }

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    if (!allowedExts.Contains(ext))
    {
        return Results.BadRequest(new { message = "Định dạng ảnh không được hỗ trợ. Chỉ hỗ trợ file JPG, PNG, WEBP, GIF." });
    }

    if (file.Length > 8 * 1024 * 1024)
    {
        return Results.BadRequest(new { message = "Dung lượng ảnh tối đa 8MB." });
    }

    var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    var dir = Path.Combine(webRoot, "uploads", "products");
    Directory.CreateDirectory(dir);

    var uniqueName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{ext}";
    var filePath = Path.Combine(dir, uniqueName);

    await using (var stream = File.Create(filePath))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new { url = $"/uploads/products/{uniqueName}" });
}).DisableAntiforgery();

app.MapPost("/api/products", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] Product p, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    if (string.IsNullOrWhiteSpace(p.ProductName))
    {
        return Results.BadRequest(new { message = "Tên sản phẩm không được để trống" });
    }
    var id = services.ProductService.CreateProduct(p, null);
    p.ProductID = id;
    services.AuditService.Log(operatorUser ?? "admin", "Thêm sản phẩm", $"Tạo mới: {p.ProductName}, Giá: {p.SellingPrice:N0}đ");
    return Results.Ok(new { message = "Thêm sản phẩm thành công", product = p });
});

app.MapPut("/api/products/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromBody] Product p, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    p.ProductID = id;
    services.ProductService.UpdateProduct(p, null);
    services.AuditService.Log(operatorUser ?? "admin", "Cập nhật sản phẩm", $"Cập nhật ID {id}: {p.ProductName}, Giá: {p.SellingPrice:N0}đ");
    return Results.Ok(new { message = "Cập nhật sản phẩm thành công" });
});

app.MapDelete("/api/products/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    services.ProductService.DeleteProduct(id);
    services.AuditService.Log(operatorUser ?? "admin", "Xoá sản phẩm", $"Xoá sản phẩm ID {id}");
    return Results.Ok(new { message = "Đã xoá sản phẩm" });
});

app.MapPost("/api/products/quick-import", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] QuickImportRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    if (req.Quantity <= 0) return Results.BadRequest(new { message = "Số lượng nhập phải lớn hơn 0" });
    var actor = string.IsNullOrWhiteSpace(operatorUser) ? "Web App" : operatorUser;
    var importId = services.ProductService.QuickImportStock(req.ProductId, req.Quantity, req.CostPrice, actor, req.Note);
    services.AuditService.Log(actor, "Nhập kho nhanh", $"SP ID {req.ProductId}, Số lượng +{req.Quantity}, Giá vốn: {req.CostPrice:N0}đ");

    string? savedHtmlFile = null;
    string? savedTxtFile = null;
    try
    {
        var product = services.ProductService.GetById(req.ProductId);
        if (product != null)
        {
            var cost = req.CostPrice ?? product.CostPrice;
            var (htmlPath, txtPath) = services.InvoiceFileService.SaveStockImportReceipt(importId, product, req.Quantity, cost, null, actor, req.Note);
            savedHtmlFile = htmlPath;
            savedTxtFile = txtPath;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[InvoiceFileService] Error saving import receipt: {ex.Message}");
    }

    return Results.Ok(new { message = "Nhập hàng thành công", importId, savedHtmlFile, savedTxtFile });
});

app.MapGet("/api/categories", ([FromServices] CoreServices services) =>
{
    return Results.Ok(services.CategoryService.GetAll());
});

app.MapPost("/api/categories", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] Category c) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    if (string.IsNullOrWhiteSpace(c.CategoryName)) return Results.BadRequest(new { message = "Tên danh mục không được rỗng" });
    var id = services.CategoryService.Create(c.CategoryName.Trim());
    return Results.Ok(new { message = "Thêm danh mục thành công", categoryId = id });
});

app.MapPut("/api/categories/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromBody] Category c) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    if (string.IsNullOrWhiteSpace(c.CategoryName)) return Results.BadRequest(new { message = "Tên danh mục không được rỗng" });
    services.CategoryService.Update(id, c.CategoryName.Trim());
    return Results.Ok(new { message = "Cập nhật danh mục thành công" });
});

app.MapDelete("/api/categories/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    services.CategoryService.Delete(id);
    return Results.Ok(new { message = "Đã xoá danh mục" });
});

// ==================== API: BÁN HÀNG, HOÁ ĐƠN & LỊCH SỬ ====================
app.MapPost("/api/sales/checkout", ([FromServices] CoreServices services, [FromBody] CheckoutRequest req, [FromQuery] string? operatorUser) =>
{
    var config = services.AppConfigService;
    if (!config.IsInvoiceFolderConfigured())
    {
        return Results.BadRequest(new
        {
            message = "Chưa thiết lập thư mục lưu trữ hoá đơn trên máy tính (HoaDon/Ban & HoaDon/Nhap). Vui lòng thiết lập thư mục trước khi thực hiện bán hàng!",
            requiresFolderSetup = true
        });
    }

    if (req.Items == null || req.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "Giỏ hàng không có sản phẩm" });
    }

    var cart = req.Items.Select(i => new CartItem
    {
        ProductID = i.ProductId,
        Barcode = i.Barcode ?? "",
        ProductName = i.ProductName,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        Note = i.Note
    }).ToList();

    var isDebt = string.Equals(req.PaymentMethod, "Ghi nợ", StringComparison.OrdinalIgnoreCase);
    int saleId;
    try
    {
        saleId = services.SalesService.SaveSale(
            cart,
            req.CustomerName,
            isDebt,
            req.DiscountAmount,
            req.DiscountNote,
            req.CustomerId);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Lỗi thanh toán", statusCode: 500);
    }

    var subtotal = cart.Sum(c => c.LineTotal);
    var finalAmount = Math.Max(0, subtotal - req.DiscountAmount);
    var changeAmount = Math.Max(0, req.ReceivedAmount - finalAmount);

    var actor = string.IsNullOrWhiteSpace(operatorUser) ? "Thu ngân" : operatorUser;
    services.AuditService.Log(actor, "Bán hàng", $"Hoá đơn #HD{saleId:D6}: {finalAmount:N0}đ ({req.PaymentMethod})");

    var saleObj = services.SalesService.GetSaleById(saleId) ?? new Sale
    {
        SaleID = saleId,
        SaleDate = DateTime.Now,
        TotalAmount = finalAmount,
        DiscountAmount = req.DiscountAmount,
        DiscountNote = req.DiscountNote,
        CustomerName = req.CustomerName,
        CustomerID = req.CustomerId,
        IsDebt = isDebt
    };

    string? savedHtmlFile = null;
    string? savedTxtFile = null;
    string? savedA4HtmlFile = null;
    try
    {
        var (htmlPath, txtPath) = services.InvoiceFileService.SaveSaleInvoice(saleObj, cart, actor);
        savedHtmlFile = htmlPath;
        savedTxtFile = txtPath;
        savedA4HtmlFile = services.InvoiceFileService.SaveSaleA4Invoice(saleObj, cart, actor);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[InvoiceFileService] Error saving sale invoice: {ex.Message}");
    }

    return Results.Ok(new
    {
        saleId,
        invoiceCode = $"HD{saleId:D6}",
        saleDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        subtotal,
        discountAmount = req.DiscountAmount,
        discountNote = req.DiscountNote,
        finalAmount,
        receivedAmount = req.ReceivedAmount,
        changeAmount,
        customerName = req.CustomerName,
        paymentMethod = req.PaymentMethod,
        items = cart,
        savedHtmlFile,
        savedTxtFile,
        savedA4HtmlFile
    });
});

app.MapGet("/api/sales/recent", ([FromServices] CoreServices services, [FromQuery] int limit = 20) =>
{
    var list = services.SalesService.GetSales(null, null, null).Take(limit).ToList();
    return Results.Ok(list);
});

app.MapGet("/api/sales/history", ([FromServices] CoreServices services, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int limit = 100) =>
{
    var list = services.SalesService.GetSales(search, fromDate, toDate).Take(limit).ToList();
    return Results.Ok(list);
});

app.MapGet("/api/sales/{id:int}/receipt", ([FromServices] CoreServices services, int id) =>
{
    using var connection = services.DatabaseContext.CreateConnection();
    var sale = Dapper.SqlMapper.QueryFirstOrDefault<Sale>(connection, "SELECT * FROM Sales WHERE SaleID = @SaleID LIMIT 1;", new { SaleID = id });
    if (sale == null) return Results.NotFound(new { message = "Không tìm thấy hoá đơn." });

    var items = services.SalesService.GetSaleDetailsForReceipt(id);
    var subtotal = items.Sum(i => i.LineTotal);
    var finalAmount = sale.TotalAmount;

    return Results.Ok(new
    {
        saleId = sale.SaleID,
        invoiceCode = $"HD{sale.SaleID:D6}",
        saleDate = sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"),
        subtotal,
        discountAmount = sale.DiscountAmount,
        discountNote = sale.DiscountNote,
        finalAmount,
        receivedAmount = finalAmount,
        changeAmount = 0,
        customerName = sale.CustomerName,
        paymentMethod = sale.IsDebt ? "Ghi nợ" : "Tiền mặt",
        items
    });
});

app.MapPost("/api/sales/{id:int}/return", ([FromServices] CoreServices services, int id, [FromBody] ReturnItemRequest req, [FromQuery] string? operatorUser) =>
{
    try
    {
        services.SalesService.ReturnItem(id, req.ProductId, req.Quantity, req.RefundAmount, req.Reason);
        var actor = string.IsNullOrWhiteSpace(operatorUser) ? "Thu ngân" : operatorUser;
        services.AuditService.Log(actor, "Trả hàng", $"Đơn #HD{id:D6}: Trả SP ID {req.ProductId} x{req.Quantity}, Hoàn tiền: {req.RefundAmount:N0}đ. Lý do: {req.Reason}");
        return Results.Ok(new { message = "Đã xử lý đổi trả hàng và hoàn tiền thành công!" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// ==================== API: KHÁCH HÀNG & CÔNG NỢ ====================
app.MapGet("/api/customers", ([FromServices] CoreServices services, [FromQuery] string? search) =>
{
    var list = string.IsNullOrWhiteSpace(search)
        ? services.CustomerService.GetAll()
        : services.CustomerService.Search(search.Trim());
    return Results.Ok(list);
});

app.MapPost("/api/customers", ([FromServices] CoreServices services, [FromBody] Customer c, [FromQuery] string? operatorUser) =>
{
    if (string.IsNullOrWhiteSpace(c.CustomerName))
    {
        return Results.BadRequest(new { message = "Tên khách hàng không được để trống" });
    }
    var id = services.CustomerService.Create(c);
    services.AuditService.Log(operatorUser ?? "admin", "Thêm khách hàng", $"{c.CustomerName} - SĐT: {c.Phone}");
    return Results.Ok(new { message = "Thêm khách hàng thành công", customerId = id });
});

app.MapPut("/api/customers/{id:int}", ([FromServices] CoreServices services, int id, [FromBody] Customer c, [FromQuery] string? operatorUser) =>
{
    c.CustomerID = id;
    services.CustomerService.Update(c);
    services.AuditService.Log(operatorUser ?? "admin", "Cập nhật khách hàng", $"ID {id}: {c.CustomerName}");
    return Results.Ok(new { message = "Cập nhật khách hàng thành công" });
});

app.MapDelete("/api/customers/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var customer = services.CustomerService.GetById(id);
    if (customer == null) return Results.NotFound(new { message = "Không tìm thấy khách hàng" });
    services.CustomerService.Delete(id);
    services.AuditService.Log(operatorUser ?? "admin", "Xóa khách hàng", $"ID {id}: {customer.CustomerName}");
    return Results.Ok(new { message = "Đã xóa khách hàng" });
});

app.MapGet("/api/customers/{id:int}/stats", ([FromServices] CoreServices services, int id) =>
{
    var customer = services.CustomerService.GetById(id);
    if (customer == null) return Results.NotFound(new { message = "Không tìm thấy khách hàng" });
    var (totalSpent, orderCount, lastPurchase) = services.CustomerService.GetCustomerStats(id);
    return Results.Ok(new
    {
        customer.CustomerID,
        customer.CustomerName,
        customer.Phone,
        totalSpent,
        orderCount,
        lastPurchase = lastPurchase?.ToString("dd/MM/yyyy HH:mm")
    });
});

app.MapGet("/api/customers/{id:int}/sales", ([FromServices] CoreServices services, int id) =>
{
    using var connection = services.DatabaseContext.CreateConnection();
    var sales = Dapper.SqlMapper.Query(connection, """
        SELECT s.SaleID, strftime('%d/%m/%Y %H:%M', s.SaleDate, 'localtime') AS SaleDate,
               COALESCE(s.TotalAmount, 0) AS TotalAmount, COALESCE(s.DiscountAmount, 0) AS DiscountAmount,
               s.DiscountNote, CASE WHEN s.IsDebt = 1 THEN 'Ghi nợ' ELSE 'Đã thanh toán' END AS PaymentStatus
        FROM Sales s
        WHERE s.CustomerID = @CustomerID
        ORDER BY s.SaleDate DESC
        LIMIT 100;
    """, new { CustomerID = id }).ToList();
    return Results.Ok(sales);
});

app.MapGet("/api/debts", ([FromServices] CoreServices services, [FromQuery] string? keyword) =>
{
    var debts = services.DebtService.GetOutstandingDebts(keyword);
    return Results.Ok(debts);
});

app.MapGet("/api/debts/{saleId:int}/payments", ([FromServices] CoreServices services, int saleId) =>
{
    var payments = services.DebtService.GetPaymentsBySale(saleId);
    return Results.Ok(payments);
});

app.MapPost("/api/debts/{saleId:int}/payments", ([FromServices] CoreServices services, int saleId, [FromBody] DebtPaymentRequest req, [FromQuery] string? operatorUser) =>
{
    if (req.Amount <= 0) return Results.BadRequest(new { message = "Số tiền trả nợ phải lớn hơn 0" });
    services.DebtService.AddPayment(saleId, req.Amount, req.Note);
    var actor = string.IsNullOrWhiteSpace(operatorUser) ? "Thu ngân" : operatorUser;
    services.AuditService.Log(actor, "Thu nợ", $"Thu nợ đơn #HD{saleId:D6}: {req.Amount:N0}đ");
    return Results.Ok(new { message = "Đã ghi nhận thanh toán nợ" });
});

// ==================== API: KHO HÀNG & BÁO CÁO ====================
app.MapGet("/api/inventory/low-stock", ([FromServices] CoreServices services) =>
{
    var products = services.ProductService.GetAll();
    var lowStock = products.Where(p => p.StockQuantity <= Math.Max(p.MinStock, 5)).ToList();
    return Results.Ok(lowStock);
});

app.MapGet("/api/inventory/import-history", ([FromServices] CoreServices services, [FromQuery] int limit = 50) =>
{
    var history = services.ProductService.GetImportHistory(null, limit);
    return Results.Ok(history);
});

app.MapGet("/api/reports/dashboard", (HttpContext ctx, [FromServices] CoreServices services) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var todayRevenue = services.SalesService.GetTodayRevenue();
    var todayCount = services.SalesService.GetTodaySaleCount();
    var monthRevenue = services.SalesService.GetCurrentMonthRevenue();
    var last7Days = services.SalesService.GetLast7DaysRevenue()
        .Select(x => new { date = x.Date.ToString("dd/MM"), revenue = x.Revenue }).ToList();
    var topSelling = services.SalesService.GetTopSellingToday(5)
        .Select(x => new { productName = x.ProductName, qty = x.TotalQty, revenue = x.TotalRevenue }).ToList();

    var debts = services.DebtService.GetOutstandingDebts(null);
    var totalDebt = debts.Sum(d => d.OutstandingAmount);

    var allProducts = services.ProductService.GetAll();
    var lowStockCount = allProducts.Count(p => p.StockQuantity <= Math.Max(p.MinStock, 5));

    return Results.Ok(new
    {
        todayRevenue,
        todayCount,
        monthRevenue,
        totalDebt,
        lowStockCount,
        productCount = allProducts.Count,
        last7Days,
        topSelling
    });
});

app.MapGet("/api/reports/profit", (HttpContext ctx, [FromServices] CoreServices services) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var today = services.SalesService.GetTodayProfitSummary();
    var month = services.SalesService.GetCurrentMonthProfitSummary();

    var todayMargin = today.RevenueAmount > 0 ? (today.ProfitAmount / today.RevenueAmount) * 100 : 0;
    var monthMargin = month.RevenueAmount > 0 ? (month.ProfitAmount / month.RevenueAmount) * 100 : 0;

    return Results.Ok(new
    {
        todayRevenue = today.RevenueAmount,
        todayCapital = today.CapitalAmount,
        todayProfit = today.ProfitAmount,
        todayMargin = Math.Round(todayMargin, 1),
        monthRevenue = month.RevenueAmount,
        monthCapital = month.CapitalAmount,
        monthProfit = month.ProfitAmount,
        monthMargin = Math.Round(monthMargin, 1)
    });
});

// ==================== THUẾ & KÊ KHAI DOANH THU ====================

// 1. Tổng hợp thuế năm (Thuế khoán: 1% GTGT + 0.5% TNCN = 1.5% doanh thu)
app.MapGet("/api/tax/summary", (HttpContext ctx, [FromServices] CoreServices services, [FromQuery] int? year) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var y = year ?? DateTime.Now.Year;
    var monthly = services.SalesService.GetMonthlyRevenueSummary(y);
    var yearly = services.SalesService.GetYearlyRevenueSummary(5);
    var totalRevenue = monthly.Sum(m => m.Revenue);
    var totalInvoices = monthly.Sum(m => m.InvoiceCount);

    // Thuế khoán theo quy định (có thể cấu hình)
    const decimal vatRate = 0.01m;      // 1% GTGT
    const decimal pitRate = 0.005m;     // 0.5% TNCN
    const decimal totalTaxRate = 0.015m; // 1.5% tổng

    // Thuế môn bài theo năm (Điều 4 Nghị định 139/2016/NĐ-CP)
    decimal businessLicenseTax = totalRevenue switch
    {
        <= 0 => 0,
        <= 100_000_000 => 0,           // Dưới 100 triệu: miễn
        <= 300_000_000 => 300_000,     // 100-300 triệu: 300.000đ/năm
        <= 500_000_000 => 500_000,     // 300-500 triệu: 500.000đ/năm
        _ => 1_000_000                  // Trên 500 triệu: 1.000.000đ/năm
    };

    var monthlyData = monthly.Select(m => new
    {
        month = m.Month,
        monthName = $"Tháng {m.Month:D2}/{y}",
        invoiceCount = m.InvoiceCount,
        revenue = m.Revenue,
        vatTax = Math.Round(m.Revenue * vatRate, 0),
        pitTax = Math.Round(m.Revenue * pitRate, 0),
        totalTax = Math.Round(m.Revenue * totalTaxRate, 0),
        quarter = (m.Month - 1) / 3 + 1
    }).ToList();

    return Results.Ok(new
    {
        year = y,
        totalRevenue,
        totalInvoices,
        vatRate = vatRate * 100,
        pitRate = pitRate * 100,
        totalTaxRate = totalTaxRate * 100,
        vatTax = Math.Round(totalRevenue * vatRate, 0),
        pitTax = Math.Round(totalRevenue * pitRate, 0),
        totalTax = Math.Round(totalRevenue * totalTaxRate, 0),
        businessLicenseTax,
        grandTotalTax = Math.Round(totalRevenue * totalTaxRate, 0) + businessLicenseTax,
        monthly = monthlyData,
        yearlyHistory = yearly
    });
});

// 2. Tổng hợp thuế theo quý
app.MapGet("/api/tax/quarterly", (HttpContext ctx, [FromServices] CoreServices services, [FromQuery] int? year) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var y = year ?? DateTime.Now.Year;
    var quarters = services.SalesService.GetQuarterlyRevenueSummary(y);
    const decimal totalTaxRate = 0.015m;

    return Results.Ok(quarters.Select(q => new
    {
        quarter = q.Quarter,
        quarterName = $"Quý {q.Quarter}/{y}",
        invoiceCount = q.InvoiceCount,
        revenue = q.Revenue,
        vatTax = Math.Round(q.Revenue * 0.01m, 0),
        pitTax = Math.Round(q.Revenue * 0.005m, 0),
        totalTax = Math.Round(q.Revenue * totalTaxRate, 0),
        fromMonth = (q.Quarter - 1) * 3 + 1,
        toMonth = q.Quarter * 3
    }));
});

// 3. Xuất CSV danh sách hoá đơn để kê khai thuế
app.MapGet("/api/tax/export-csv", (HttpContext ctx, [FromServices] CoreServices services,
    [FromQuery] string? from, [FromQuery] string? to, [FromQuery] int? year, [FromQuery] int? month) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();

    DateTime fromDate, toDate;
    if (year.HasValue && month.HasValue)
    {
        fromDate = new DateTime(year.Value, month.Value, 1);
        toDate = fromDate.AddMonths(1).AddSeconds(-1);
    }
    else if (year.HasValue)
    {
        fromDate = new DateTime(year.Value, 1, 1);
        toDate = new DateTime(year.Value, 12, 31, 23, 59, 59);
    }
    else
    {
        fromDate = DateTime.TryParse(from, out var fd) ? fd : new DateTime(DateTime.Now.Year, 1, 1);
        toDate = DateTime.TryParse(to, out var td) ? td : DateTime.Now;
    }

    var rows = services.SalesService.GetTaxInvoiceRows(fromDate, toDate);
    var config = services.AppConfigService;
    var storeName = config.StoreName ?? "Cửa hàng";
    var taxId = config.StoreTaxId ?? "";

    // Tạo nội dung CSV
    var csvLines = new System.Text.StringBuilder();
    csvLines.AppendLine("BÁO CÁO DOANH THU - KÊ KHAI THUẾ");
    csvLines.AppendLine($"Cơ sở kinh doanh:,{storeName}");
    csvLines.AppendLine($"Mã số thuế:,{taxId}");
    csvLines.AppendLine($"Kỳ báo cáo:,{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}");
    csvLines.AppendLine($"Ngày xuất:,{DateTime.Now:dd/MM/yyyy HH:mm}");
    csvLines.AppendLine();
    csvLines.AppendLine("STT,Ngày,Giờ,Số hoá đơn,Khách hàng,Doanh thu (đ),Chiết khấu (đ),Số mặt hàng,Hình thức TT,Thuế GTGT 1% (đ),Thuế TNCN 0.5% (đ),Tổng thuế 1.5% (đ)");

    int stt = 1;
    decimal totalRevenue = 0, totalVat = 0, totalPit = 0;
    foreach (var r in rows)
    {
        var vat = Math.Round(r.Revenue * 0.01m, 0);
        var pit = Math.Round(r.Revenue * 0.005m, 0);
        totalRevenue += r.Revenue;
        totalVat += vat;
        totalPit += pit;
        csvLines.AppendLine($"{stt++},{r.SaleDate},{r.SaleTime},HD{r.SaleID:D6},\"{r.CustomerName}\",{r.Revenue},{r.DiscountAmount},{r.ItemCount},{r.PaymentMethod},{vat},{pit},{vat + pit}");
    }

    csvLines.AppendLine();
    csvLines.AppendLine($"TỔNG CỘNG,,,,, {totalRevenue},,,, {totalVat},{totalPit},{totalVat + totalPit}");
    csvLines.AppendLine();
    csvLines.AppendLine("Ghi chú:,Thuế khoán theo Nghị định 126/2020/NĐ-CP và Thông tư 40/2021/TT-BTC");
    csvLines.AppendLine("Tỷ lệ thuế:,GTGT 1% + TNCN 0.5% = 1.5% doanh thu (áp dụng cho hộ kinh doanh khoán)");

    var periodLabel = year.HasValue && month.HasValue
        ? $"Thang{month:D2}_{year}"
        : $"{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
    var fileName = $"KhaiThue_{periodLabel}_{storeName.Replace(" ", "_")}.csv";

    var bytes = System.Text.Encoding.UTF8.GetPreamble()
        .Concat(System.Text.Encoding.UTF8.GetBytes(csvLines.ToString())).ToArray();

    return Results.File(bytes, "text/csv; charset=utf-8", fileName);
});

// 4. Báo cáo tóm tắt thuế để in (JSON)
app.MapGet("/api/tax/print-summary", (HttpContext ctx, [FromServices] CoreServices services,
    [FromQuery] int? year, [FromQuery] int? quarter) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var y = year ?? DateTime.Now.Year;
    var config = services.AppConfigService;

    DateTime from, to;
    string periodLabel;
    if (quarter.HasValue)
    {
        from = new DateTime(y, (quarter.Value - 1) * 3 + 1, 1);
        to = from.AddMonths(3).AddSeconds(-1);
        periodLabel = $"Quý {quarter}/{y}";
    }
    else
    {
        from = new DateTime(y, 1, 1);
        to = new DateTime(y, 12, 31, 23, 59, 59);
        periodLabel = $"Năm {y}";
    }

    var rows = services.SalesService.GetTaxInvoiceRows(from, to);
    var totalRevenue = rows.Sum(r => r.Revenue);
    var vatTax = Math.Round(totalRevenue * 0.01m, 0);
    var pitTax = Math.Round(totalRevenue * 0.005m, 0);

    return Results.Ok(new
    {
        storeName = config.StoreName,
        storeAddress = config.StoreAddress,
        storePhone = config.StorePhone,
        taxId = config.StoreTaxId,
        periodLabel,
        from = from.ToString("dd/MM/yyyy"),
        to = to.ToString("dd/MM/yyyy"),
        invoiceCount = rows.Count,
        totalRevenue,
        vatTax,
        pitTax,
        totalTax = vatTax + pitTax,
        printDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
    });
});



app.MapGet("/api/inventory/expiring", ([FromServices] CoreServices services) =>
{
    var products = services.ProductService.GetAll();
    var thresholdDays = services.AppConfigService.NearExpiryWarningDays > 0 ? services.AppConfigService.NearExpiryWarningDays : 30;
    var targetDate = DateTime.Today.AddDays(thresholdDays);

    var expiring = products
        .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= targetDate)
        .OrderBy(p => p.ExpiryDate)
        .Select(p => new
        {
            p.ProductID,
            p.Barcode,
            p.ProductName,
            p.CategoryName,
            p.Unit,
            p.SellingPrice,
            p.StockQuantity,
            expiryDate = p.ExpiryDate!.Value.ToString("yyyy-MM-dd"),
            daysRemaining = (int)(p.ExpiryDate!.Value.Date - DateTime.Today).TotalDays,
            isExpired = p.ExpiryDate!.Value.Date < DateTime.Today
        })
        .ToList();

    return Results.Ok(expiring);
});

// ==================== API: QUẢN LÝ NGƯỜI DÙNG & AUTH ====================
app.MapGet("/api/users", (HttpContext ctx, [FromServices] CoreServices services) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var users = services.UserService.GetAll().Select(u => new
    {
        u.UserID,
        u.Username,
        u.FullName,
        u.Role,
        u.IsActive
    });
    return Results.Ok(users);
});

app.MapPost("/api/users", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] CreateUserRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    try
    {
        var id = services.UserService.Create(req.Username, req.FullName, req.Role, req.Password);
        services.AuditService.Log(operatorUser ?? "admin", "Tạo tài khoản", $"Tài khoản mới: {req.Username} ({req.Role})");
        return Results.Ok(new { message = "Tạo người dùng thành công", userId = id });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPut("/api/users/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromBody] UpdateUserRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    try
    {
        services.UserService.UpdateRoleAndStatus(id, req.Role, req.IsActive);
        services.AuditService.Log(operatorUser ?? "admin", "Cập nhật tài khoản", $"ID {id}: Role={req.Role}, Active={req.IsActive}");
        return Results.Ok(new { message = "Cập nhật tài khoản thành công" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/users/{id:int}/reset-password", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromBody] ResetPasswordRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    try
    {
        services.UserService.ResetPassword(id, req.NewPassword);
        services.AuditService.Log(operatorUser ?? "admin", "Đặt lại mật khẩu", $"Đặt lại mật khẩu cho User ID {id}");
        return Results.Ok(new { message = "Đặt lại mật khẩu thành công" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapDelete("/api/users/{id:int}", (HttpContext ctx, [FromServices] CoreServices services, int id, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    try
    {
        services.UserService.Delete(id);
        services.AuditService.Log(operatorUser ?? "admin", "Xoá tài khoản", $"Xoá tài khoản ID {id}");
        return Results.Ok(new { message = "Đã xoá tài khoản" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/auth/login", ([FromServices] CoreServices services, [FromBody] LoginRequest req) =>
{
    var user = services.AuthService.Authenticate(req.Username, req.Password);
    if (user == null)
    {
        return Results.BadRequest(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
    }
    services.AuditService.Log(user.Username, "Đăng nhập", $"Người dùng {user.Username} ({user.Role}) đăng nhập vào Web App");
    return Results.Ok(new
    {
        userId = user.UserID,
        username = user.Username,
        fullName = user.FullName,
        role = user.Role
    });
});

app.MapPost("/api/auth/change-password", ([FromServices] CoreServices services, [FromBody] ChangePasswordRequest req) =>
{
    try
    {
        var ok = services.AuthService.ChangePassword(req.UserId, req.CurrentPassword, req.NewPassword);
        if (!ok) return Results.BadRequest(new { message = "Mật khẩu hiện tại không đúng." });
        services.AuditService.Log($"User#{req.UserId}", "Đổi mật khẩu", "Đổi mật khẩu thành công");
        return Results.Ok(new { message = "Đổi mật khẩu thành công!" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// ==================== API: NHẬT KÝ THAO TÁC (AUDIT LOGS) ====================
app.MapGet("/api/audit-logs", (HttpContext ctx, [FromServices] CoreServices services, [FromQuery] int limit = 100) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var logs = services.AuditService.GetRecent(limit);
    return Results.Ok(logs);
});

// ==================== API: SAO LƯU & KHÔI PHỤC DATABASE ====================
app.MapGet("/api/database/backup", (HttpContext ctx, [FromServices] CoreServices services) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var dbPath = services.DatabaseContext.DatabaseFilePath;
    if (!File.Exists(dbPath)) return Results.NotFound(new { message = "Không tìm thấy file database." });

    services.AuditService.Log("system", "Sao lưu dữ liệu", "Tải file backup database SQLite");
    var fileName = $"taphoa_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
    return Results.File(dbPath, "application/x-sqlite3", fileName);
});

app.MapPost("/api/database/restore", async (HttpContext ctx, [FromServices] CoreServices services, IFormFile file) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "Vui lòng chọn file backup hợp lệ (.db)." });
    }

    var tempPath = Path.GetTempFileName();
    try
    {
        await using (var stream = File.Create(tempPath))
        {
            await file.CopyToAsync(stream);
        }

        services.BackupService.RestoreFrom(tempPath);
        services.AuditService.Log("system", "Khôi phục dữ liệu", $"Khôi phục từ file {file.FileName}");
        return Results.Ok(new { message = "Khôi phục dữ liệu thành công! Vui lòng làm mới trang." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = "Lỗi khôi phục: " + ex.Message });
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}).DisableAntiforgery();

// ==================== API: THƯ MỤC HOÁ ĐƠN THEO NGÀY (HoaDon/Ban & HoaDon/Nhap) ====================
app.MapGet("/api/invoice-folder/status", ([FromServices] CoreServices services) =>
{
    var cfg = services.AppConfigService;
    var currentPath = cfg.InvoiceStoragePath;
    var defaultPath = AppConfigService.GetDefaultInvoiceStoragePath();
    var isConfigured = cfg.IsInvoiceFolderConfigured();
    var banExists = !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(Path.Combine(currentPath, "Ban"));
    var nhapExists = !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(Path.Combine(currentPath, "Nhap"));

    return Results.Ok(new
    {
        isConfigured,
        currentPath,
        defaultPath,
        banExists,
        nhapExists,
        canSell = isConfigured
    });
});

app.MapPost("/api/invoice-folder/setup", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] InvoiceFolderSetupRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var cfg = services.AppConfigService;
    var path = cfg.SetupInvoiceFolder(req?.Path);
    services.AuditService.Log(operatorUser ?? "admin", "Cấu hình thư mục hoá đơn", $"Thiết lập thư mục: {path}");
    return Results.Ok(new
    {
        message = "Đã khởi tạo thư mục lưu hoá đơn thành công (HoaDon/Ban & HoaDon/Nhap)!",
        path,
        banPath = Path.Combine(path, "Ban"),
        nhapPath = Path.Combine(path, "Nhap"),
        isConfigured = true
    });
});

app.MapPost("/api/invoice-folder/open", ([FromServices] CoreServices services, [FromQuery] string? folder) =>
{
    var cfg = services.AppConfigService;
    var root = cfg.InvoiceStoragePath;
    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
    {
        return Results.BadRequest(new { message = "Thư mục chưa được thiết lập hoặc không tồn tại trên máy tính." });
    }

    var targetPath = root;
    if (string.Equals(folder, "ban", StringComparison.OrdinalIgnoreCase))
    {
        var ban = Path.Combine(root, "Ban");
        if (Directory.Exists(ban)) targetPath = ban;
    }
    else if (string.Equals(folder, "nhap", StringComparison.OrdinalIgnoreCase))
    {
        var nhap = Path.Combine(root, "Nhap");
        if (Directory.Exists(nhap)) targetPath = nhap;
    }

    try
    {
        if (OperatingSystem.IsMacOS())
        {
            System.Diagnostics.Process.Start("open", targetPath);
        }
        else if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", targetPath) { UseShellExecute = true });
        }
        else if (OperatingSystem.IsLinux())
        {
            System.Diagnostics.Process.Start("xdg-open", targetPath);
        }
        return Results.Ok(new { message = $"Đã mở thư mục: {targetPath}", path = targetPath });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"Không thể mở thư mục: {ex.Message}" });
    }
});

// ==================== API: CÀI ĐẶT & MÁY IN & VIETQR ====================
app.MapGet("/api/settings", ([FromServices] CoreServices services) =>
{
    var cfg = services.AppConfigService;
    return Results.Ok(new
    {
        storeName = cfg.StoreName,
        storeAddress = cfg.StoreAddress,
        storePhone = cfg.StorePhone,
        receiptFooter = cfg.ReceiptFooter,
        defaultPaperWidth = cfg.DefaultPaperWidth,
        autoPrintReceipt = cfg.AutoPrintReceipt,
        defaultPrinter = cfg.DefaultPrinter,
        printerModel = cfg.PrinterModel,
        printerConnectionType = cfg.PrinterConnectionType,
        printCopies = cfg.PrintCopies,
        printQrOnReceipt = cfg.PrintQrOnReceipt,
        receiptFontSize = cfg.ReceiptFontSize,
        scannerType = cfg.ScannerType,
        scannerModel = cfg.ScannerModel,
        scannerSuffix = cfg.ScannerSuffix,
        scannerBeepSound = cfg.ScannerBeepSound,
        scannerAutoAdd = cfg.ScannerAutoAdd,
        storeTaxId = cfg.StoreTaxId,
        a4InvoiceTitle = cfg.A4InvoiceTitle,
        a4PaperSize = cfg.A4PaperSize,
        a4Orientation = cfg.A4Orientation,
        a4ShowSignatures = cfg.A4ShowSignatures,
        a4ShowBankQr = cfg.A4ShowBankQr,
        qrPaymentEnabled = cfg.QrPaymentEnabled,
        qrBankBin = cfg.QrBankBin,
        qrAccountNo = cfg.QrAccountNo,
        qrAccountName = cfg.QrAccountName,
        qrTransferPrefix = cfg.QrTransferPrefix,
        paymentAutoDetectMode = cfg.PaymentAutoDetectMode,
        paymentWebhookSecret = cfg.PaymentWebhookSecret,
        paymentSoundEnabled = cfg.PaymentSoundEnabled,
        invoiceStoragePath = cfg.InvoiceStoragePath,
        isInvoiceFolderConfigured = cfg.IsInvoiceFolderConfigured(),
        defaultInvoicePath = AppConfigService.GetDefaultInvoiceStoragePath(),
        esp32ConnMode = cfg.Esp32ConnMode,
        esp32ServerIp = cfg.Esp32ServerIp,
        esp32WifiSsid = cfg.Esp32WifiSsid,
        esp32WifiPass = cfg.Esp32WifiPass
    });
});

var handleUpdateSettings = (HttpContext ctx, [FromServices] CoreServices services, [FromBody] SettingsUpdateRequest req, [FromQuery] string? operatorUser) =>
{
    if (!IsAdmin(ctx, services)) return ForbiddenResult();
    var cfg = services.AppConfigService;
    if (req.StoreName != null) cfg.StoreName = req.StoreName;
    if (req.StoreAddress != null) cfg.StoreAddress = req.StoreAddress;
    if (req.StorePhone != null) cfg.StorePhone = req.StorePhone;
    if (req.ReceiptFooter != null) cfg.ReceiptFooter = req.ReceiptFooter;
    if (req.DefaultPaperWidth.HasValue) cfg.DefaultPaperWidth = req.DefaultPaperWidth.Value;
    if (req.AutoPrintReceipt.HasValue) cfg.AutoPrintReceipt = req.AutoPrintReceipt.Value;
    if (req.DefaultPrinter != null) cfg.DefaultPrinter = req.DefaultPrinter;
    if (req.PrinterModel != null) cfg.PrinterModel = req.PrinterModel;
    if (req.PrinterConnectionType != null) cfg.PrinterConnectionType = req.PrinterConnectionType;
    if (req.PrintCopies.HasValue) cfg.PrintCopies = req.PrintCopies.Value;
    if (req.PrintQrOnReceipt.HasValue) cfg.PrintQrOnReceipt = req.PrintQrOnReceipt.Value;
    if (req.ReceiptFontSize != null) cfg.ReceiptFontSize = req.ReceiptFontSize;
    if (req.ScannerType != null) cfg.ScannerType = req.ScannerType;
    if (req.ScannerModel != null) cfg.ScannerModel = req.ScannerModel;
    if (req.ScannerSuffix != null) cfg.ScannerSuffix = req.ScannerSuffix;
    if (req.ScannerBeepSound.HasValue) cfg.ScannerBeepSound = req.ScannerBeepSound.Value;
    if (req.ScannerAutoAdd.HasValue) cfg.ScannerAutoAdd = req.ScannerAutoAdd.Value;
    if (req.StoreTaxId != null) cfg.StoreTaxId = req.StoreTaxId;
    if (req.A4InvoiceTitle != null) cfg.A4InvoiceTitle = req.A4InvoiceTitle;
    if (req.A4PaperSize != null) cfg.A4PaperSize = req.A4PaperSize;
    if (req.A4Orientation != null) cfg.A4Orientation = req.A4Orientation;
    if (req.A4ShowSignatures.HasValue) cfg.A4ShowSignatures = req.A4ShowSignatures.Value;
    if (req.A4ShowBankQr.HasValue) cfg.A4ShowBankQr = req.A4ShowBankQr.Value;
    if (req.QrPaymentEnabled.HasValue) cfg.QrPaymentEnabled = req.QrPaymentEnabled.Value;
    if (req.QrBankBin != null) cfg.QrBankBin = req.QrBankBin;
    if (req.QrAccountNo != null) cfg.QrAccountNo = req.QrAccountNo;
    if (req.QrAccountName != null) cfg.QrAccountName = req.QrAccountName;
    if (req.QrTransferPrefix != null) cfg.QrTransferPrefix = req.QrTransferPrefix;
    if (req.PaymentAutoDetectMode != null) cfg.PaymentAutoDetectMode = req.PaymentAutoDetectMode;
    if (req.PaymentWebhookSecret != null) cfg.PaymentWebhookSecret = req.PaymentWebhookSecret;
    if (req.PaymentSoundEnabled.HasValue) cfg.PaymentSoundEnabled = req.PaymentSoundEnabled.Value;
    if (req.InvoiceStoragePath != null) cfg.SetupInvoiceFolder(req.InvoiceStoragePath);
    if (req.Esp32ConnMode != null) cfg.Esp32ConnMode = req.Esp32ConnMode;
    if (req.Esp32ServerIp != null) cfg.Esp32ServerIp = req.Esp32ServerIp;
    if (req.Esp32WifiSsid != null) cfg.Esp32WifiSsid = req.Esp32WifiSsid;
    if (req.Esp32WifiPass != null) cfg.Esp32WifiPass = req.Esp32WifiPass;

    services.AuditService.Log(operatorUser ?? "admin", "Cài đặt", "Cập nhật cấu hình hệ thống, phần cứng, máy in & thanh toán");
    return Results.Ok(new { message = "Lưu cấu hình thành công" });
};

app.MapPost("/api/settings", handleUpdateSettings);
app.MapPut("/api/settings", handleUpdateSettings);


app.MapGet("/api/sales/{id:int}/invoice-a4", ([FromServices] CoreServices services, int id, [FromQuery] string? operatorUser) =>
{
    var sale = services.SalesService.GetSaleById(id);
    if (sale == null) return Results.NotFound(new { message = "Không tìm thấy hoá đơn." });
    var items = services.SalesService.GetSaleDetailsForReceipt(id);
    var cashier = string.IsNullOrWhiteSpace(operatorUser) ? "Thu ngân" : operatorUser;
    var html = services.InvoiceFileService.GenerateSaleA4Html(sale, items, cashier);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/api/sales/{id:int}/invoice-thermal", ([FromServices] CoreServices services, int id, [FromQuery] string? operatorUser) =>
{
    var sale = services.SalesService.GetSaleById(id);
    if (sale == null) return Results.NotFound(new { message = "Không tìm thấy hoá đơn." });
    var items = services.SalesService.GetSaleDetailsForReceipt(id);
    var cashier = string.IsNullOrWhiteSpace(operatorUser) ? "Thu ngân" : operatorUser;
    var html = services.InvoiceFileService.GenerateSaleHtml(sale, items, cashier);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/api/vietqr", ([FromServices] CoreServices services, [FromQuery] decimal amount, [FromQuery] string? memo) =>
{
    var cfg = services.AppConfigService;
    if (!cfg.QrPaymentEnabled || string.IsNullOrWhiteSpace(cfg.QrBankBin) || string.IsNullOrWhiteSpace(cfg.QrAccountNo))
    {
        return Results.BadRequest(new { message = "Cửa hàng chưa cấu hình VietQR." });
    }

    var bank = cfg.QrBankBin.Trim();
    var acc = cfg.QrAccountNo.Trim();
    var name = Uri.EscapeDataString(cfg.QrAccountName ?? "");
    var content = Uri.EscapeDataString(string.IsNullOrWhiteSpace(memo) ? (cfg.QrTransferPrefix ?? "HD") : memo);
    var intAmount = (long)Math.Max(0, amount);

    var qrUrl = $"https://img.vietqr.io/image/{bank}-{acc}-compact2.png?amount={intAmount}&addInfo={content}&accountName={name}";
    return Results.Ok(new
    {
        qrUrl,
        bankBin = bank,
        accountNo = acc,
        accountName = cfg.QrAccountName,
        amount = intAmount,
        content = memo
    });
});

// ==================== TỰ ĐỘNG NHẬN DIỆN THANH TOÁN VIETQR ====================
app.MapPost("/api/payment/create-pending", ([FromServices] CoreServices services, [FromBody] CreatePendingPaymentRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.PaymentCode) || req.Amount <= 0)
    {
        return Results.BadRequest(new { message = "Mã thanh toán và số tiền không hợp lệ." });
    }
    var pending = services.PaymentAutomationService.CreatePendingPayment(req.PaymentCode, req.Amount, req.CustomerName);
    return Results.Ok(pending);
});

app.MapGet("/api/payment/status/{code}", ([FromServices] CoreServices services, string code) =>
{
    var p = services.PaymentAutomationService.GetPayment(code);
    if (p == null)
    {
        return Results.Ok(new { status = "NotFound", isSuccess = false });
    }
    return Results.Ok(new
    {
        paymentCode = p.PaymentCode,
        status = p.Status.ToString(),
        isSuccess = p.Status == PaymentStatus.Success,
        expectedAmount = p.ExpectedAmount,
        receivedAmount = p.ReceivedAmount,
        paidAt = p.PaidAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        bankName = p.BankName,
        transactionRef = p.TransactionRef,
        source = p.Source
    });
});

// GIẢI PHÁP 1: Cổng Webhook trực tuyến SePay / Casso / Ngân hàng
app.MapPost("/api/payment/webhook", async (HttpContext ctx, [FromServices] CoreServices services) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var rawJson = await reader.ReadToEndAsync();
    var secret = ctx.Request.Headers["Authorization"].ToString();
    var (success, message, matched) = services.PaymentAutomationService.ProcessWebhook(rawJson, secret);

    return Results.Ok(new
    {
        success,
        message,
        matchedCode = matched?.PaymentCode,
        amount = matched?.ReceivedAmount
    });
});

// GIẢI PHÁP 2: Chuyển tiếp tin nhắn SMS / Thông báo app ngân hàng từ điện thoại
app.MapPost("/api/payment/phone-notification", async (HttpContext ctx, [FromServices] CoreServices services) =>
{
    string text = "";
    string? bank = null;

    if (ctx.Request.HasJsonContentType())
    {
        try
        {
            var req = await ctx.Request.ReadFromJsonAsync<PhoneNotificationRequest>();
            text = req?.Text ?? "";
            bank = req?.Bank;
        }
        catch { }
    }
    else
    {
        using var reader = new StreamReader(ctx.Request.Body);
        text = await reader.ReadToEndAsync();
    }

    var (success, message, matched) = services.PaymentAutomationService.ProcessPhoneNotification(text, bank);
    return Results.Ok(new
    {
        success,
        message,
        matchedCode = matched?.PaymentCode,
        amount = matched?.ReceivedAmount
    });
});

// API hỗ trợ nhân viên xác nhận chuyển khoản khi không có kết nối webhook (kiểm tra thủ công)
app.MapPost("/api/payment/simulate", ([FromServices] CoreServices services, [FromBody] SimulatePaymentRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.PaymentCode))
    {
        return Results.BadRequest(new { message = "Vui lòng truyền mã thanh toán cần giả lập." });
    }
    var (success, message, matched) = services.PaymentAutomationService.SimulateTransfer(req.PaymentCode, req.Amount, req.Bank);
    return Results.Ok(new
    {
        success,
        message,
        matchedCode = matched?.PaymentCode,
        amount = matched?.ReceivedAmount
    });
});

app.MapGet("/api/payment/logs", ([FromServices] CoreServices services) =>
{
    var logs = services.PaymentAutomationService.GetRecentLogs();
    return Results.Ok(logs);
});

// ==================== MÀN HÌNH PHỤ KHÁCH HÀNG (ESP32 + TFT 1.8 SPI 128x160) ====================
// 1. ESP32 board poll dữ liệu màn hình
app.MapGet("/api/esp32/display", ([FromServices] CoreServices services) =>
{
    var info = services.Esp32CustomerDisplayService.GetCurrentScreenInfo();
    return Results.Ok(info);
});

// 2. Lấy thông tin trạng thái màn hình ESP32 cho Web POS
app.MapGet("/api/esp32/status", (HttpContext ctx, [FromServices] CoreServices services) =>
{
    var dev = services.Esp32CustomerDisplayService.DeviceStatus;
    var screen = services.Esp32CustomerDisplayService.GetCurrentScreenInfo();
    var serverIp = GetLocalIpAddress(ctx);
    return Results.Ok(new
    {
        isOnline = dev.IsOnline,
        ipAddress = dev.IpAddress,
        rssi = dev.Rssi,
        lastPing = dev.LastPingUtc != DateTime.MinValue ? dev.LastPingUtc.ToString("yyyy-MM-dd HH:mm:ss") : null,
        firmwareVersion = dev.FirmwareVersion,
        screenResolution = dev.ScreenResolution,
        currentScreen = screen,
        serverIp
    });
});

// 3. Heartbeat từ ESP32
app.MapPost("/api/esp32/ping", (HttpContext ctx, [FromServices] CoreServices services, [FromBody] Esp32PingRequest req) =>
{
    var ip = req.Ip ?? ctx.Connection.RemoteIpAddress?.ToString();
    services.Esp32CustomerDisplayService.RecordPing(ip, req.Rssi, req.Version);
    return Results.Ok(new { success = true });
});

// 4. POS hoặc Admin điều khiển trạng thái màn hình ESP32 (IDLE, QR, SUCCESS)
app.MapPost("/api/esp32/set-state", ([FromServices] CoreServices services, [FromBody] Esp32SetStateRequest req) =>
{
    if (string.Equals(req.State, "QR", StringComparison.OrdinalIgnoreCase))
    {
        services.Esp32CustomerDisplayService.SetWaitingPayment(req.OrderCode ?? "HDTEST", req.Amount ?? 150000, req.CustomerName);
    }
    else if (string.Equals(req.State, "SUCCESS", StringComparison.OrdinalIgnoreCase))
    {
        services.Esp32CustomerDisplayService.SetPaymentSuccess(req.OrderCode ?? "HDTEST", req.Amount ?? 150000, req.BankName);
    }
    else
    {
        services.Esp32CustomerDisplayService.SetIdle(req.Message);
    }
    return Results.Ok(services.Esp32CustomerDisplayService.GetCurrentScreenInfo());
});

// 5. Tải file mã nguồn Arduino .ino hoàn chỉnh nạp cho ESP32 (Chế độ WiFi)
app.MapGet("/api/esp32/arduino-code", (HttpContext ctx, [FromServices] CoreServices services, [FromQuery] string? ssid, [FromQuery] string? pass, [FromQuery] string? serverIp) =>
{
    var ip = serverIp;
    if (string.IsNullOrWhiteSpace(ip))
    {
        ip = GetLocalIpAddress(ctx);
    }
    var code = services.Esp32CustomerDisplayService.GenerateArduinoCode(ip, ssid ?? "WiFi_CuaHang", pass ?? "matkhau123");
    return Results.Content(code, "text/plain; charset=utf-8");
});

// 6. Tải file mã nguồn Arduino .ino kết nối TRỰC TIẾP QUA CỔNG USB (Không dùng WiFi, cắm dây USB là chạy)
app.MapGet("/api/esp32/arduino-usb-code", ([FromServices] CoreServices services) =>
{
    var code = services.Esp32CustomerDisplayService.GenerateArduinoUsbCode();
    return Results.Content(code, "text/plain; charset=utf-8");
});

static string GetLocalIpAddress(HttpContext? ctx = null)
{
    try
    {
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
        if (endPoint != null) return endPoint.Address.ToString();
    }
    catch { }

    if (ctx != null)
    {
        var host = ctx.Request.Host.Host;
        if (!string.IsNullOrWhiteSpace(host) && host != "localhost" && host != "127.0.0.1") return host;
    }

    return "192.168.1.100";
}

Console.WriteLine("==================================================================");
Console.WriteLine(" PM TẠP HOÁ - WEB APP LOCAL SẴN SÀNG");
Console.WriteLine(" Mở trình duyệt trên máy Mac và truy cập:");
Console.WriteLine("   -> http://localhost:5050");
Console.WriteLine("   -> http://127.0.0.1:5050");
Console.WriteLine("==================================================================");

app.Run();

// DTOs
public record CreatePendingPaymentRequest(string PaymentCode, decimal Amount, string? CustomerName);
public record PhoneNotificationRequest(string Text, string? Bank);
public record SimulatePaymentRequest(string PaymentCode, decimal? Amount, string? Bank);
public record QuickImportRequest(int ProductId, double Quantity, decimal? CostPrice, string? Note);
public record CartItemDto(int ProductId, string? Barcode, string ProductName, int Quantity, decimal UnitPrice, string? Note);
public record CheckoutRequest(
    int? CustomerId,
    string? CustomerName,
    string PaymentMethod,
    decimal DiscountAmount,
    string? DiscountNote,
    decimal ReceivedAmount,
    List<CartItemDto> Items);
public record DebtPaymentRequest(decimal Amount, string? Note);
public record ReturnItemRequest(int ProductId, int Quantity, decimal RefundAmount, string? Reason);
public record CreateUserRequest(string Username, string FullName, string Role, string Password);
public record UpdateUserRequest(string Role, bool IsActive);
public record ResetPasswordRequest(string NewPassword);
public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(int UserId, string CurrentPassword, string NewPassword);
public record SettingsUpdateRequest(
    string? StoreName,
    string? StoreAddress,
    string? StorePhone,
    string? ReceiptFooter,
    int? DefaultPaperWidth,
    bool? AutoPrintReceipt,
    string? DefaultPrinter,
    string? PrinterModel,
    string? PrinterConnectionType,
    int? PrintCopies,
    bool? PrintQrOnReceipt,
    string? ReceiptFontSize,
    string? ScannerType,
    string? ScannerModel,
    string? ScannerSuffix,
    bool? ScannerBeepSound,
    bool? ScannerAutoAdd,
    string? StoreTaxId,
    string? A4InvoiceTitle,
    string? A4PaperSize,
    string? A4Orientation,
    bool? A4ShowSignatures,
    bool? A4ShowBankQr,
    bool? QrPaymentEnabled,
    string? QrBankBin,
    string? QrAccountNo,
    string? QrAccountName,
    string? QrTransferPrefix,
    string? PaymentAutoDetectMode,
    string? PaymentWebhookSecret,
    bool? PaymentSoundEnabled,
    string? InvoiceStoragePath,
    string? Esp32ConnMode = null,
    string? Esp32ServerIp = null,
    string? Esp32WifiSsid = null,
    string? Esp32WifiPass = null);
public record InvoiceFolderSetupRequest(string? Path);
public record Esp32PingRequest(string? Ip, int? Rssi, string? Version);
public record Esp32SetStateRequest(string State, string? OrderCode, decimal? Amount, string? CustomerName, string? BankName, string? Message);
