using System.Globalization;
using System.Text;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class InvoiceFileService
{
    private readonly AppConfigService _config;
    private static readonly CultureInfo VnCulture = new("vi-VN");

    public InvoiceFileService(AppConfigService config)
    {
        _config = config;
    }

    /// <summary>
    /// Lưu file hoá đơn bán hàng (HTML & TXT) vào thư mục: [InvoiceStoragePath]/Ban/YYYY-MM-DD/
    /// </summary>
    public (string HtmlPath, string TxtPath) SaveSaleInvoice(Sale sale, List<CartItem> items, string? cashierName = null)
    {
        var root = _config.InvoiceStoragePath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            root = _config.SetupInvoiceFolder();
        }

        var now = DateTime.Now;
        var dateFolder = now.ToString("yyyy-MM-dd");
        var banDir = Path.Combine(root, "Ban", dateFolder);
        Directory.CreateDirectory(banDir);

        var timeStamp = now.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"HD{sale.SaleID:D6}_{timeStamp}";
        var htmlPath = Path.Combine(banDir, $"{baseFileName}.html");
        var txtPath = Path.Combine(banDir, $"{baseFileName}.txt");

        var htmlContent = GenerateSaleHtml(sale, items, cashierName, now);
        File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

        var txtContent = GenerateSaleTxt(sale, items, cashierName, now);
        File.WriteAllText(txtPath, txtContent, Encoding.UTF8);

        return (htmlPath, txtPath);
    }

    /// <summary>
    /// Lưu file phiếu nhập kho (HTML & TXT) vào thư mục: [InvoiceStoragePath]/Nhap/YYYY-MM-DD/
    /// </summary>
    public (string HtmlPath, string TxtPath) SaveStockImportReceipt(
        int importId, Product product, double quantity, decimal costPrice,
        string? supplierName = null, string? importedBy = null, string? note = null)
    {
        var root = _config.InvoiceStoragePath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            root = _config.SetupInvoiceFolder();
        }

        var now = DateTime.Now;
        var dateFolder = now.ToString("yyyy-MM-dd");
        var nhapDir = Path.Combine(root, "Nhap", dateFolder);
        Directory.CreateDirectory(nhapDir);

        var timeStamp = now.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"PN{importId:D6}_{timeStamp}";
        var htmlPath = Path.Combine(nhapDir, $"{baseFileName}.html");
        var txtPath = Path.Combine(nhapDir, $"{baseFileName}.txt");

        var htmlContent = GenerateStockImportHtml(importId, product, quantity, costPrice, supplierName, importedBy, note, now);
        File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

        var txtContent = GenerateStockImportTxt(importId, product, quantity, costPrice, supplierName, importedBy, note, now);
        File.WriteAllText(txtPath, txtContent, Encoding.UTF8);

        return (htmlPath, txtPath);
    }

    public string GenerateSaleHtml(Sale sale, List<CartItem> items, string? cashierName = null, DateTime? printTime = null)
    {
        var now = printTime ?? (sale.SaleDate != default ? sale.SaleDate : DateTime.Now);
        var storeName = System.Net.WebUtility.HtmlEncode(_config.StoreName);
        var storeAddress = System.Net.WebUtility.HtmlEncode(_config.StoreAddress);
        var storePhone = System.Net.WebUtility.HtmlEncode(_config.StorePhone);
        var footer = System.Net.WebUtility.HtmlEncode(_config.ReceiptFooter);
        var customer = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(sale.CustomerName) ? "Khách lẻ" : sale.CustomerName);
        var cashier = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(cashierName) ? "Nhân viên" : cashierName);
        var discountNote = System.Net.WebUtility.HtmlEncode(sale.DiscountNote ?? string.Empty);
        var is58 = _config.DefaultPaperWidth == 58;
        var printCopies = _config.PrintCopies;
        var showQr = _config.PrintQrOnReceipt && _config.QrPaymentEnabled && !string.IsNullOrWhiteSpace(_config.QrBankBin) && !string.IsNullOrWhiteSpace(_config.QrAccountNo);

        string? qrUrl = null;
        if (showQr)
        {
            var bank = _config.QrBankBin.Trim();
            var acc = _config.QrAccountNo.Trim();
            var name = Uri.EscapeDataString(_config.QrAccountName ?? "");
            var content = Uri.EscapeDataString($"HD{sale.SaleID:D6}");
            var intAmount = (long)Math.Max(0, sale.TotalAmount);
            qrUrl = $"https://img.vietqr.io/image/{bank}-{acc}-qr_only.png?amount={intAmount}&addInfo={content}";
        }

        var subtotal = items.Sum(i => i.LineTotal);
        var discount = sale.DiscountAmount;
        var total = sale.TotalAmount;
        var paymentMethod = sale.IsDebt ? "Ghi nợ" : "Tiền mặt / Chuyển khoản";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>Hóa đơn bán lẻ #HD{sale.SaleID:D6}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f8fafc; color: #1e293b; padding: 16px; }");
        sb.AppendLine($"    .invoice-card {{ max-width: {(is58 ? "380px" : "480px")}; margin: 0 auto; background: #fff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); padding: 20px; border: 1px solid #e2e8f0; }}");
        sb.AppendLine("    .header { text-align: center; border-bottom: 2px dashed #cbd5e1; padding-bottom: 14px; margin-bottom: 14px; }");
        sb.AppendLine($"    .store-name {{ font-size: {(is58 ? "16px" : "19px")}; font-weight: 800; color: #0f172a; text-transform: uppercase; margin-bottom: 4px; }}");
        sb.AppendLine("    .store-info { font-size: 12.5px; color: #64748b; margin-bottom: 2px; }");
        sb.AppendLine($"    .invoice-title {{ font-size: {(is58 ? "15px" : "17px")}; font-weight: 700; margin-top: 10px; color: #0284c7; letter-spacing: 0.5px; }}");
        sb.AppendLine("    .meta-grid { display: grid; grid-template-columns: 1fr 1fr; font-size: 12px; margin-bottom: 14px; row-gap: 4px; }");
        sb.AppendLine("    .meta-grid div:nth-child(even) { text-align: right; }");
        sb.AppendLine("    .meta-label { color: #64748b; }");
        sb.AppendLine("    .meta-val { font-weight: 600; color: #1e293b; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 12.5px; margin-bottom: 14px; }");
        sb.AppendLine("    th { text-align: left; padding: 6px 4px; border-bottom: 2px solid #e2e8f0; color: #475569; font-weight: 600; font-size: 11.5px; }");
        sb.AppendLine("    td { padding: 6px 4px; border-bottom: 1px solid #f1f5f9; }");
        sb.AppendLine("    .text-right { text-align: right; }");
        sb.AppendLine("    .text-center { text-align: center; }");
        sb.AppendLine("    .summary-row { display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 5px; }");
        sb.AppendLine("    .total-row { display: flex; justify-content: space-between; font-size: 16px; font-weight: 800; color: #0f172a; border-top: 2px dashed #cbd5e1; padding-top: 10px; margin-top: 6px; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 2px 6px; border-radius: 4px; font-size: 10.5px; font-weight: 700; }");
        sb.AppendLine("    .badge-paid { background: #ecfdf5; color: #059669; border: 1px solid #a7f3d0; }");
        sb.AppendLine("    .badge-debt { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; }");
        sb.AppendLine("    .footer { text-align: center; margin-top: 16px; border-top: 1px solid #f1f5f9; padding-top: 12px; font-size: 12px; color: #64748b; font-style: italic; }");
        sb.AppendLine("    .actions { text-align: center; margin-top: 16px; }");
        sb.AppendLine("    .btn-print { background: #0284c7; color: #fff; border: none; padding: 8px 18px; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer; }");
        sb.AppendLine("    .copy-divider { border-top: 2px dashed #94a3b8; margin: 24px 0; text-align: center; font-size: 11px; color: #64748b; padding-top: 6px; }");
        sb.AppendLine("    @media print {");
        sb.AppendLine("      body { background: #fff; padding: 0; }");
        sb.AppendLine("      .invoice-card { box-shadow: none; border: none; max-width: 100%; padding: 0; }");
        sb.AppendLine("      .actions { display: none !important; }");
        sb.AppendLine("      .page-break { page-break-after: always; }");
        sb.AppendLine("    }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        for (int copy = 1; copy <= printCopies; copy++)
        {
            if (copy > 1)
            {
                sb.AppendLine("  <div class=\"copy-divider page-break\">--- CẮT GIẤY / LIÊN 2 (LƯU NỘI BỘ) ---</div>");
            }

            sb.AppendLine("  <div class=\"invoice-card\">");
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine($"      <div class=\"store-name\">{storeName}</div>");
            if (!string.IsNullOrWhiteSpace(storeAddress))
                sb.AppendLine($"      <div class=\"store-info\">{storeAddress}</div>");
            if (!string.IsNullOrWhiteSpace(storePhone))
                sb.AppendLine($"      <div class=\"store-info\">Hotline: {storePhone}</div>");
            sb.AppendLine($"      <div class=\"invoice-title\">HÓA ĐƠN BÁN LẺ</div>");
            sb.AppendLine($"      <div style=\"font-size:12px; color:#64748b; margin-top:2px;\">Số: <b>#HD{sale.SaleID:D6}</b> {(printCopies > 1 ? (copy == 1 ? "<i>(Liên 1: Khách)</i>" : "<i>(Liên 2: Quầy)</i>") : "")}</div>");
            sb.AppendLine("    </div>");

            sb.AppendLine("    <div class=\"meta-grid\">");
            sb.AppendLine($"      <div><span class=\"meta-label\">Thời gian:</span> <span class=\"meta-val\">{now:dd/MM/yyyy HH:mm:ss}</span></div>");
            sb.AppendLine($"      <div><span class=\"meta-label\">Thu ngân:</span> <span class=\"meta-val\">{cashier}</span></div>");
            sb.AppendLine($"      <div><span class=\"meta-label\">Khách hàng:</span> <span class=\"meta-val\">{customer}</span></div>");
            sb.AppendLine($"      <div><span class=\"meta-label\">Trạng thái:</span> <span class=\"badge {(sale.IsDebt ? "badge-debt" : "badge-paid")}\">{paymentMethod}</span></div>");
            sb.AppendLine("    </div>");

            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead>");
            sb.AppendLine("        <tr>");
            sb.AppendLine("          <th style=\"width:30px;\" class=\"text-center\">#</th>");
            sb.AppendLine("          <th>Mặt hàng</th>");
            sb.AppendLine("          <th class=\"text-center\" style=\"width:35px;\">SL</th>");
            sb.AppendLine("          <th class=\"text-right\" style=\"width:70px;\">Đơn giá</th>");
            sb.AppendLine("          <th class=\"text-right\" style=\"width:80px;\">T.Tiền</th>");
            sb.AppendLine("        </tr>");
            sb.AppendLine("      </thead>");
            sb.AppendLine("      <tbody>");

            int stt = 1;
            foreach (var item in items)
            {
                var pName = System.Net.WebUtility.HtmlEncode(item.ProductName);
                sb.AppendLine("        <tr>");
                sb.AppendLine($"          <td class=\"text-center\">{stt++}</td>");
                sb.AppendLine($"          <td><b>{pName}</b>{(string.IsNullOrWhiteSpace(item.Note) ? "" : $"<br><small style=\"color:#64748b;\">({System.Net.WebUtility.HtmlEncode(item.Note)})</small>")}</td>");
                sb.AppendLine($"          <td class=\"text-center\">{item.Quantity}</td>");
                sb.AppendLine($"          <td class=\"text-right\">{item.UnitPrice.ToString("N0", VnCulture)}</td>");
                sb.AppendLine($"          <td class=\"text-right\"><b>{item.LineTotal.ToString("N0", VnCulture)}</b></td>");
                sb.AppendLine("        </tr>");
            }

            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");

            sb.AppendLine("    <div>");
            sb.AppendLine($"      <div class=\"summary-row\"><span>Tổng tiền hàng:</span> <span>{subtotal.ToString("N0", VnCulture)} đ</span></div>");
            if (discount > 0)
            {
                sb.AppendLine($"      <div class=\"summary-row\" style=\"color:#e11d48;\"><span>Giảm giá{(string.IsNullOrWhiteSpace(discountNote) ? "" : $" ({discountNote})")}:</span> <span>-{discount.ToString("N0", VnCulture)} đ</span></div>");
            }
            sb.AppendLine($"      <div class=\"total-row\"><span>THANH TOÁN:</span> <span style=\"color:#0284c7;\">{total.ToString("N0", VnCulture)} đ</span></div>");
            sb.AppendLine("    </div>");

            if (showQr && !string.IsNullOrWhiteSpace(qrUrl))
            {
                sb.AppendLine("    <div style=\"text-align:center; margin:10px 0 6px 0; padding:6px;\">");
                sb.AppendLine($"      <img src=\"{qrUrl}\" style=\"width:130px; height:130px; border:none; background:#fff; margin:0 auto; display:block;\" alt=\"QR\">");
                sb.AppendLine("    </div>");
            }

            sb.AppendLine($"    <div class=\"footer\">{footer}</div>");
            if (copy == 1)
            {
                sb.AppendLine("    <div class=\"actions\"><button class=\"btn-print\" onclick=\"window.print()\">🖨 In lại hóa đơn này</button></div>");
            }
            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateSaleTxt(Sale sale, List<CartItem> items, string? cashierName, DateTime printTime)
    {
        var sb = new StringBuilder();
        var storeName = _config.StoreName;
        var cashier = string.IsNullOrWhiteSpace(cashierName) ? "Nhân viên" : cashierName;
        var customer = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Khách lẻ" : sale.CustomerName;

        sb.AppendLine("========================================");
        sb.AppendLine($"       {storeName.ToUpper()}");
        if (!string.IsNullOrWhiteSpace(_config.StoreAddress))
            sb.AppendLine($" {_config.StoreAddress}");
        if (!string.IsNullOrWhiteSpace(_config.StorePhone))
            sb.AppendLine($" SĐT: {_config.StorePhone}");
        sb.AppendLine("========================================");
        sb.AppendLine($"           HÓA ĐƠN BÁN LẺ");
        sb.AppendLine($"Số HĐ:   #HD{sale.SaleID:D6}");
        sb.AppendLine($"Ngày:    {printTime:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Thu ngân: {cashier}");
        sb.AppendLine($"Khách:   {customer}");
        sb.AppendLine($"H.thức:  {(sale.IsDebt ? "Ghi nợ" : "Tiền mặt / Chuyển khoản")}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine(string.Format("{0,-18} {1,4} {2,8} {3,8}", "Tên hàng", "SL", "Đ.Giá", "T.Tiền"));
        sb.AppendLine("----------------------------------------");

        foreach (var item in items)
        {
            var name = item.ProductName;
            if (name.Length > 18) name = name.Substring(0, 16) + "..";
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,8:N0} {3,8:N0}",
                name, item.Quantity, item.UnitPrice, item.LineTotal));
        }

        sb.AppendLine("----------------------------------------");
        var subtotal = items.Sum(i => i.LineTotal);
        sb.AppendLine($"Tổng tiền hàng:   {subtotal.ToString("N0", VnCulture),18} đ");
        if (sale.DiscountAmount > 0)
        {
            sb.AppendLine($"Giảm giá:         -{sale.DiscountAmount.ToString("N0", VnCulture),17} đ");
        }
        sb.AppendLine($"THANH TOÁN:       {sale.TotalAmount.ToString("N0", VnCulture),18} đ");
        sb.AppendLine("========================================");
        sb.AppendLine($"  {_config.ReceiptFooter}");
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private string GenerateStockImportHtml(
        int importId, Product product, double quantity, decimal costPrice,
        string? supplierName, string? importedBy, string? note, DateTime printTime)
    {
        var storeName = System.Net.WebUtility.HtmlEncode(_config.StoreName);
        var pName = System.Net.WebUtility.HtmlEncode(product.ProductName);
        var barcode = System.Net.WebUtility.HtmlEncode(product.Barcode);
        var unit = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(product.Unit) ? "Cái" : product.Unit);
        var supplier = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(supplierName) ? "Nhà cung cấp trực tiếp" : supplierName);
        var importer = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(importedBy) ? "Quản trị viên" : importedBy);
        var noteText = System.Net.WebUtility.HtmlEncode(note ?? string.Empty);
        var totalCost = (decimal)quantity * costPrice;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>Phiếu nhập hàng #PN{importId:D6}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f8fafc; color: #1e293b; padding: 20px; }");
        sb.AppendLine("    .invoice-card { max-width: 520px; margin: 0 auto; background: #fff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); padding: 24px; border: 1px solid #e2e8f0; }");
        sb.AppendLine("    .header { text-align: center; border-bottom: 2px dashed #cbd5e1; padding-bottom: 16px; margin-bottom: 16px; }");
        sb.AppendLine("    .store-name { font-size: 20px; font-weight: 800; color: #0f172a; text-transform: uppercase; margin-bottom: 4px; }");
        sb.AppendLine("    .invoice-title { font-size: 17px; font-weight: 700; margin-top: 10px; color: #16a34a; letter-spacing: 0.5px; }");
        sb.AppendLine("    .meta-grid { display: grid; grid-template-columns: 1fr 1fr; font-size: 12.5px; margin-bottom: 16px; row-gap: 5px; }");
        sb.AppendLine("    .meta-grid div:nth-child(even) { text-align: right; }");
        sb.AppendLine("    .meta-label { color: #64748b; }");
        sb.AppendLine("    .meta-val { font-weight: 600; color: #1e293b; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 16px; }");
        sb.AppendLine("    th { text-align: left; padding: 8px 4px; border-bottom: 2px solid #e2e8f0; color: #475569; font-weight: 600; font-size: 12px; }");
        sb.AppendLine("    td { padding: 8px 4px; border-bottom: 1px solid #f1f5f9; }");
        sb.AppendLine("    .text-right { text-align: right; }");
        sb.AppendLine("    .text-center { text-align: center; }");
        sb.AppendLine("    .total-row { display: flex; justify-content: space-between; font-size: 17px; font-weight: 800; color: #0f172a; border-top: 2px dashed #cbd5e1; padding-top: 10px; margin-top: 6px; }");
        sb.AppendLine("    .footer { text-align: center; margin-top: 20px; border-top: 1px solid #f1f5f9; padding-top: 14px; font-size: 12px; color: #64748b; }");
        sb.AppendLine("    .actions { text-align: center; margin-top: 16px; }");
        sb.AppendLine("    .btn-print { background: #16a34a; color: #fff; border: none; padding: 8px 18px; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer; }");
        sb.AppendLine("    @media print {");
        sb.AppendLine("      body { background: #fff; padding: 0; }");
        sb.AppendLine("      .invoice-card { box-shadow: none; border: none; max-width: 100%; padding: 0; }");
        sb.AppendLine("      .actions { display: none !important; }");
        sb.AppendLine("    }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"invoice-card\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine($"      <div class=\"store-name\">{storeName}</div>");
        sb.AppendLine($"      <div class=\"invoice-title\">PHIẾU NHẬP HÀNG KHO</div>");
        sb.AppendLine($"      <div style=\"font-size:12px; color:#64748b; margin-top:2px;\">Mã phiếu: <b>#PN{importId:D6}</b></div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <div class=\"meta-grid\">");
        sb.AppendLine($"      <div><span class=\"meta-label\">Thời gian:</span> <span class=\"meta-val\">{printTime:dd/MM/yyyy HH:mm:ss}</span></div>");
        sb.AppendLine($"      <div><span class=\"meta-label\">Người nhập:</span> <span class=\"meta-val\">{importer}</span></div>");
        sb.AppendLine($"      <div><span class=\"meta-label\">Nhà cung cấp:</span> <span class=\"meta-val\">{supplier}</span></div>");
        sb.AppendLine($"      <div><span class=\"meta-label\">Mã vạch:</span> <span class=\"meta-val\">{barcode}</span></div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("          <th>Sản phẩm</th>");
        sb.AppendLine("          <th class=\"text-center\" style=\"width:45px;\">ĐVT</th>");
        sb.AppendLine("          <th class=\"text-center\" style=\"width:50px;\">SL nhập</th>");
        sb.AppendLine("          <th class=\"text-right\" style=\"width:80px;\">Giá nhập</th>");
        sb.AppendLine("          <th class=\"text-right\" style=\"width:90px;\">Thành tiền</th>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"          <td><b>{pName}</b>{(string.IsNullOrWhiteSpace(noteText) ? "" : $"<br><small style=\"color:#64748b;\">Ghi chú: {noteText}</small>")}</td>");
        sb.AppendLine($"          <td class=\"text-center\">{unit}</td>");
        sb.AppendLine($"          <td class=\"text-center\">{quantity}</td>");
        sb.AppendLine($"          <td class=\"text-right\">{costPrice.ToString("N0", VnCulture)}</td>");
        sb.AppendLine($"          <td class=\"text-right\"><b>{totalCost.ToString("N0", VnCulture)}</b></td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        sb.AppendLine("    <div>");
        sb.AppendLine($"      <div class=\"total-row\"><span>TỔNG TIỀN NHẬP:</span> <span style=\"color:#16a34a;\">{totalCost.ToString("N0", VnCulture)} đ</span></div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <div class=\"footer\">Phiếu lưu trữ tự động trong hệ thống quản lý kho tạp hóa.</div>");
        sb.AppendLine("    <div class=\"actions\"><button class=\"btn-print\" onclick=\"window.print()\">🖨 In lại phiếu này</button></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateStockImportTxt(
        int importId, Product product, double quantity, decimal costPrice,
        string? supplierName, string? importedBy, string? note, DateTime printTime)
    {
        var sb = new StringBuilder();
        var storeName = _config.StoreName;
        var totalCost = (decimal)quantity * costPrice;

        sb.AppendLine("========================================");
        sb.AppendLine($"       {storeName.ToUpper()}");
        sb.AppendLine("========================================");
        sb.AppendLine($"          PHIẾU NHẬP HÀNG KHO");
        sb.AppendLine($"Mã phiếu: #PN{importId:D6}");
        sb.AppendLine($"Ngày:     {printTime:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Người nhập:{importedBy ?? "Admin"}");
        sb.AppendLine($"NCC:      {supplierName ?? "Trực tiếp"}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"Mặt hàng: {product.ProductName}");
        sb.AppendLine($"Mã vạch:  {product.Barcode}");
        sb.AppendLine($"Số lượng: {quantity} {product.Unit}");
        sb.AppendLine($"Giá nhập: {costPrice.ToString("N0", VnCulture)} đ");
        sb.AppendLine($"Thành tiền:{totalCost.ToString("N0", VnCulture)} đ");
        if (!string.IsNullOrWhiteSpace(note))
        {
            sb.AppendLine($"Ghi chú:  {note}");
        }
        sb.AppendLine("========================================");
        return sb.ToString();
    }

    /// <summary>
    /// Lưu file hoá đơn tổng A4/A5 vào thư mục: [InvoiceStoragePath]/Ban/YYYY-MM-DD/
    /// </summary>
    public string SaveSaleA4Invoice(Sale sale, List<CartItem> items, string? cashierName = null)
    {
        var root = _config.InvoiceStoragePath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            root = _config.SetupInvoiceFolder();
        }

        var now = DateTime.Now;
        var dateFolder = now.ToString("yyyy-MM-dd");
        var banDir = Path.Combine(root, "Ban", dateFolder);
        Directory.CreateDirectory(banDir);

        var timeStamp = now.ToString("yyyyMMdd_HHmmss");
        var a4Path = Path.Combine(banDir, $"HD{sale.SaleID:D6}_{timeStamp}_A4.html");

        var htmlContent = GenerateSaleA4Html(sale, items, cashierName, now);
        File.WriteAllText(a4Path, htmlContent, Encoding.UTF8);

        return a4Path;
    }

    /// <summary>
    /// Sinh mã HTML hoá đơn bán lẻ / bán buôn tổng khổ A4 hoặc A5 chuẩn mực in ấn văn phòng
    /// </summary>
    public string GenerateSaleA4Html(Sale sale, List<CartItem> items, string? cashierName = null, DateTime? printTime = null)
    {
        var now = printTime ?? (sale.SaleDate != default ? sale.SaleDate : DateTime.Now);
        var storeName = System.Net.WebUtility.HtmlEncode(_config.StoreName);
        var storeAddress = System.Net.WebUtility.HtmlEncode(_config.StoreAddress);
        var storePhone = System.Net.WebUtility.HtmlEncode(_config.StorePhone);
        var storeTaxId = System.Net.WebUtility.HtmlEncode(_config.StoreTaxId);
        var title = System.Net.WebUtility.HtmlEncode(_config.A4InvoiceTitle);
        var customer = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(sale.CustomerName) ? "Khách mua lẻ" : sale.CustomerName);
        var cashier = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(cashierName) ? "Quản lý / Thu ngân" : cashierName);
        var paperSize = _config.A4PaperSize.ToLower() == "a5" ? "A5" : "A4";
        var orientation = _config.A4Orientation.ToLower() == "landscape" ? "landscape" : "portrait";
        var showSignatures = _config.A4ShowSignatures;
        var showQr = _config.A4ShowBankQr && _config.QrPaymentEnabled && !string.IsNullOrWhiteSpace(_config.QrBankBin) && !string.IsNullOrWhiteSpace(_config.QrAccountNo);

        var subtotal = items.Sum(i => i.LineTotal);
        var discount = sale.DiscountAmount;
        var total = sale.TotalAmount;
        var amountInWords = VietnameseNumberReader.ToWords(total);
        var paymentMethod = sale.IsDebt ? "Ghi nợ (Chưa thanh toán)" : "Đã thanh toán (Tiền mặt / Chuyển khoản)";

        string? qrUrl = null;
        if (showQr)
        {
            var bank = _config.QrBankBin.Trim();
            var acc = _config.QrAccountNo.Trim();
            var name = Uri.EscapeDataString(_config.QrAccountName ?? "");
            var content = Uri.EscapeDataString($"HD{sale.SaleID:D6}");
            var intAmount = (long)Math.Max(0, total);
            qrUrl = $"https://img.vietqr.io/image/{bank}-{acc}-qr_only.png?amount={intAmount}&addInfo={content}";
        }

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>{title} #HD{sale.SaleID:D6}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine($"    @page {{ size: {paperSize} {orientation}; margin: 12mm 15mm; }}");
        sb.AppendLine("    body { font-family: 'Times New Roman', Times, serif; color: #111827; background: #f3f4f6; padding: 20px; font-size: 14px; line-height: 1.4; }");
        sb.AppendLine("    .a4-container { max-width: 800px; margin: 0 auto; background: #fff; padding: 36px 40px; border-radius: 8px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }");
        sb.AppendLine("    .header-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        sb.AppendLine("    .header-table td { vertical-align: top; }");
        sb.AppendLine("    .store-name { font-size: 18px; font-weight: bold; text-transform: uppercase; color: #0f172a; margin-bottom: 4px; }");
        sb.AppendLine("    .store-info { font-size: 13.5px; color: #374151; margin-bottom: 2px; }");
        sb.AppendLine("    .invoice-meta { text-align: right; font-size: 13.5px; }");
        sb.AppendLine("    .invoice-meta b { color: #0284c7; }");
        sb.AppendLine("    .main-title { text-align: center; margin: 18px 0 6px 0; }");
        sb.AppendLine("    .main-title h1 { font-size: 22px; font-weight: bold; text-transform: uppercase; letter-spacing: 1px; color: #1e293b; }");
        sb.AppendLine("    .main-title .date-line { font-style: italic; font-size: 13px; color: #4b5563; }");
        sb.AppendLine("    .customer-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px 16px; margin-bottom: 20px; font-size: 14px; }");
        sb.AppendLine("    .customer-grid { display: grid; grid-template-columns: 1.2fr 1fr; row-gap: 6px; }");
        sb.AppendLine("    table.items-table { width: 100%; border-collapse: collapse; margin-bottom: 16px; font-size: 13.5px; }");
        sb.AppendLine("    table.items-table th, table.items-table td { border: 1px solid #cbd5e1; padding: 8px 10px; }");
        sb.AppendLine("    table.items-table th { background: #f1f5f9; font-weight: bold; text-align: center; text-transform: uppercase; font-size: 12.5px; }");
        sb.AppendLine("    .text-center { text-align: center; }");
        sb.AppendLine("    .text-right { text-align: right; }");
        sb.AppendLine("    .total-section { width: 100%; margin-bottom: 14px; font-size: 14px; }");
        sb.AppendLine("    .words-box { background: #fdf4ff; border: 1px dashed #d946ef; padding: 8px 12px; border-radius: 6px; font-size: 13.5px; margin-bottom: 18px; }");
        sb.AppendLine("    .qr-and-sign { display: flex; justify-content: space-between; align-items: flex-start; margin-top: 10px; }");
        sb.AppendLine("    .signatures-grid { width: 100%; display: grid; grid-template-columns: 1fr 1fr 1fr; text-align: center; margin-top: 20px; }");
        sb.AppendLine("    .sign-title { font-weight: bold; font-size: 14px; margin-bottom: 4px; }");
        sb.AppendLine("    .sign-sub { font-style: italic; font-size: 12px; color: #6b7280; margin-bottom: 60px; }");
        sb.AppendLine("    .sign-name { font-weight: bold; font-size: 13px; }");
        sb.AppendLine("    .btn-bar { text-align: center; margin: 20px 0; }");
        sb.AppendLine("    .btn-print { background: #0284c7; color: #fff; border: none; padding: 10px 24px; border-radius: 6px; font-size: 14px; font-weight: bold; cursor: pointer; }");
        sb.AppendLine("    @media print {");
        sb.AppendLine("      body { background: #fff; padding: 0; }");
        sb.AppendLine("      .a4-container { box-shadow: none; border: none; padding: 0; max-width: 100%; }");
        sb.AppendLine("      .btn-bar { display: none !important; }");
        sb.AppendLine("    }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"btn-bar\">");
        sb.AppendLine("    <button class=\"btn-print\" onclick=\"window.print()\">🖨 IN HOÁ ĐƠN NÀY (Ctrl+P)</button>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"a4-container\">");

        // Header
        sb.AppendLine("    <table class=\"header-table\">");
        sb.AppendLine("      <tr>");
        sb.AppendLine("        <td style=\"width: 65%;\">");
        sb.AppendLine($"          <div class=\"store-name\">{storeName}</div>");
        if (!string.IsNullOrWhiteSpace(storeAddress))
            sb.AppendLine($"          <div class=\"store-info\"><b>Địa chỉ:</b> {storeAddress}</div>");
        if (!string.IsNullOrWhiteSpace(storePhone))
            sb.AppendLine($"          <div class=\"store-info\"><b>Điện thoại:</b> {storePhone}</div>");
        if (!string.IsNullOrWhiteSpace(storeTaxId))
            sb.AppendLine($"          <div class=\"store-info\"><b>Mã số thuế:</b> {storeTaxId}</div>");
        sb.AppendLine("        </td>");
        sb.AppendLine("        <td class=\"invoice-meta\">");
        sb.AppendLine($"          <div>Số HĐ: <b>#HD{sale.SaleID:D6}</b></div>");
        sb.AppendLine($"          <div>Ngày: {now:dd/MM/yyyy}</div>");
        sb.AppendLine($"          <div>Giờ: {now:HH:mm:ss}</div>");
        sb.AppendLine($"          <div>Thu ngân: {cashier}</div>");
        sb.AppendLine("        </td>");
        sb.AppendLine("      </tr>");
        sb.AppendLine("    </table>");

        // Main Title
        sb.AppendLine("    <div class=\"main-title\">");
        sb.AppendLine($"      <h1>{title}</h1>");
        sb.AppendLine($"      <div class=\"date-line\">Ngày {now:dd} tháng {now:MM} năm {now:yyyy}</div>");
        sb.AppendLine("    </div>");

        // Customer Box
        sb.AppendLine("    <div class=\"customer-box\">");
        sb.AppendLine("      <div class=\"customer-grid\">");
        sb.AppendLine($"        <div><b>Khách hàng / Đơn vị:</b> {customer}</div>");
        sb.AppendLine($"        <div><b>Hình thức:</b> {paymentMethod}</div>");
        sb.AppendLine($"        <div><b>Mã đơn hàng:</b> HD{sale.SaleID:D6}</div>");
        sb.AppendLine($"        <div><b>Trạng thái:</b> {(sale.IsDebt ? "<span style='color:red; font-weight:bold;'>Ghi nợ sổ</span>" : "<span style='color:green; font-weight:bold;'>Đã thu đủ tiền</span>")}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Table
        sb.AppendLine("    <table class=\"items-table\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("          <th style=\"width: 35px;\">STT</th>");
        sb.AppendLine("          <th>Tên mặt hàng / Quy cách</th>");
        sb.AppendLine("          <th style=\"width: 50px;\">ĐVT</th>");
        sb.AppendLine("          <th style=\"width: 50px;\">SL</th>");
        sb.AppendLine("          <th style=\"width: 95px;\" class=\"text-right\">Đơn giá (đ)</th>");
        sb.AppendLine("          <th style=\"width: 110px;\" class=\"text-right\">Thành tiền (đ)</th>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        int stt = 1;
        foreach (var item in items)
        {
            var pName = System.Net.WebUtility.HtmlEncode(item.ProductName);
            sb.AppendLine("        <tr>");
            sb.AppendLine($"          <td class=\"text-center\">{stt++}</td>");
            sb.AppendLine($"          <td><b>{pName}</b>{(string.IsNullOrWhiteSpace(item.Note) ? "" : $" <span style=\"color:#6b7280; font-style:italic;\">({System.Net.WebUtility.HtmlEncode(item.Note)})</span>")}</td>");
            sb.AppendLine($"          <td class=\"text-center\">Cái</td>");
            sb.AppendLine($"          <td class=\"text-center\"><b>{item.Quantity}</b></td>");
            sb.AppendLine($"          <td class=\"text-right\">{item.UnitPrice.ToString("N0", VnCulture)}</td>");
            sb.AppendLine($"          <td class=\"text-right\"><b>{item.LineTotal.ToString("N0", VnCulture)}</b></td>");
            sb.AppendLine("        </tr>");
        }

        // Summary Rows
        sb.AppendLine("        <tr>");
        sb.AppendLine("          <td colspan=\"5\" class=\"text-right\" style=\"font-weight: bold;\">Tổng cộng tiền hàng:</td>");
        sb.AppendLine($"          <td class=\"text-right\" style=\"font-weight: bold;\">{subtotal.ToString("N0", VnCulture)}</td>");
        sb.AppendLine("        </tr>");

        if (discount > 0)
        {
            var dNote = System.Net.WebUtility.HtmlEncode(sale.DiscountNote ?? "Chiết khấu");
            sb.AppendLine("        <tr>");
            sb.AppendLine($"          <td colspan=\"5\" class=\"text-right\" style=\"color: #dc2626;\">Chiết khấu / Giảm giá ({dNote}):</td>");
            sb.AppendLine($"          <td class=\"text-right\" style=\"color: #dc2626;\">-{discount.ToString("N0", VnCulture)}</td>");
            sb.AppendLine("        </tr>");
        }

        sb.AppendLine("        <tr style=\"background: #f8fafc;\">");
        sb.AppendLine("          <td colspan=\"5\" class=\"text-right\" style=\"font-size: 15px; font-weight: bold; color: #0284c7;\">TỔNG TIỀN THANH TOÁN:</td>");
        sb.AppendLine($"          <td class=\"text-right\" style=\"font-size: 16px; font-weight: bold; color: #0284c7;\">{total.ToString("N0", VnCulture)} đ</td>");
        sb.AppendLine("        </tr>");

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        // Amount in words
        sb.AppendLine("    <div class=\"words-box\">");
        sb.AppendLine($"      <b>Số tiền viết bằng chữ:</b> <i>{amountInWords}</i>");
        sb.AppendLine("    </div>");

        // QR Code (if enabled)
        if (showQr && !string.IsNullOrWhiteSpace(qrUrl))
        {
            sb.AppendLine("    <div style=\"text-align:center; margin:14px 0; padding:10px;\">");
            sb.AppendLine($"      <img src=\"{qrUrl}\" style=\"width:110px; height:110px; object-fit:contain; border:none; margin:0 auto; display:block;\" alt=\"QR\">");
            sb.AppendLine("    </div>");
        }

        // Signatures
        if (showSignatures)
        {
            sb.AppendLine("    <div class=\"signatures-grid\">");
            sb.AppendLine("      <div>");
            sb.AppendLine("        <div class=\"sign-title\">NGƯỜI MUA HÀNG</div>");
            sb.AppendLine("        <div class=\"sign-sub\">(Ký, ghi rõ họ tên)</div>");
            sb.AppendLine($"        <div class=\"sign-name\">{customer}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div>");
            sb.AppendLine("        <div class=\"sign-title\">NGƯỜI GIAO HÀNG</div>");
            sb.AppendLine("        <div class=\"sign-sub\">(Ký, ghi rõ họ tên)</div>");
            sb.AppendLine("        <div class=\"sign-name\">...............................</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div>");
            sb.AppendLine("        <div class=\"sign-title\">NGƯỜI LẬP PHIẾU</div>");
            sb.AppendLine("        <div class=\"sign-sub\">(Ký, ghi rõ họ tên)</div>");
            sb.AppendLine($"        <div class=\"sign-name\">{cashier}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
