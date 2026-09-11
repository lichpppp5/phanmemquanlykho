using ClosedXML.Excel;
using PMTapHoa.Desktop.Models;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class ExportService
{
    // ── Hóa đơn ────────────────────────────────────────────────────────────────
    public void ExportSalesToExcel(string filePath, List<SaleHistoryItem> sales)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Danh sach hoa don");
        SetHeader(ws, "Mã HĐ", "Ngày bán", "Khách hàng", "Tổng tiền", "Ghi nợ");

        for (var i = 0; i < sales.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = sales[i].SaleID;
            ws.Cell(row, 2).Value = sales[i].SaleDate;
            ws.Cell(row, 3).Value = sales[i].CustomerName ?? string.Empty;
            ws.Cell(row, 4).Value = sales[i].TotalAmount;
            ws.Cell(row, 5).Value = sales[i].IsDebt ? "Có" : "Không";
        }

        FormatWorksheet(ws, sales.Count);
        workbook.SaveAs(filePath);
    }

    // ── Công nợ ─────────────────────────────────────────────────────────────────
    public void ExportOutstandingDebtsToExcel(string filePath, List<DebtSaleItem> debts)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Cong no");
        SetHeader(ws, "Mã HĐ", "Ngày bán", "Khách hàng", "SĐT", "Tổng nợ", "Đã trả", "Còn lại");

        for (var i = 0; i < debts.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = debts[i].SaleID;
            ws.Cell(row, 2).Value = debts[i].SaleDate;
            ws.Cell(row, 3).Value = debts[i].CustomerName ?? string.Empty;
            ws.Cell(row, 4).Value = debts[i].CustomerPhone ?? string.Empty;
            ws.Cell(row, 5).Value = debts[i].TotalAmount;
            ws.Cell(row, 6).Value = debts[i].PaidAmount;
            ws.Cell(row, 7).Value = debts[i].OutstandingAmount;
        }

        FormatWorksheet(ws, debts.Count);
        workbook.SaveAs(filePath);
    }

    // ── Báo cáo lợi nhuận ──────────────────────────────────────────────────────
    public void ExportProfitReportToExcel(string filePath,
        decimal revenue, decimal capital, decimal profit,
        DateTime? from, DateTime? to,
        List<(string ProductName, int TotalQty, decimal TotalRevenue)> topSelling)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Bao cao loi nhuan");

        ws.Cell(1, 1).Value = "BÁO CÁO LỢI NHUẬN";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = from.HasValue ? $"Từ: {from:dd/MM/yyyy}" : "Từ: Đầu";
        ws.Cell(3, 1).Value = to.HasValue ? $"Đến: {to:dd/MM/yyyy}" : "Đến: Hôm nay";
        ws.Cell(5, 1).Value = "Doanh thu"; ws.Cell(5, 2).Value = revenue;
        ws.Cell(6, 1).Value = "Giá vốn"; ws.Cell(6, 2).Value = capital;
        ws.Cell(7, 1).Value = "Lợi nhuận"; ws.Cell(7, 2).Value = profit;
        ws.Cell(7, 1).Style.Font.Bold = true;
        ws.Cell(7, 2).Style.Font.Bold = true;

        ws.Cell(9, 1).Value = "TOP SẢN PHẨM BÁN CHẠY";
        ws.Cell(9, 1).Style.Font.Bold = true;
        SetHeaderAt(ws, 10, "Tên sản phẩm", "Số lượng bán", "Doanh thu");

        for (var i = 0; i < topSelling.Count; i++)
        {
            var row = 11 + i;
            ws.Cell(row, 1).Value = topSelling[i].ProductName;
            ws.Cell(row, 2).Value = topSelling[i].TotalQty;
            ws.Cell(row, 3).Value = topSelling[i].TotalRevenue;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    // ── Tồn kho ─────────────────────────────────────────────────────────────────
    public void ExportInventoryToExcel(string filePath, List<Product> products)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ton kho");
        SetHeader(ws, "Mã vạch", "Tên sản phẩm", "Danh mục", "ĐVT", "Tồn kho", "Tối thiểu", "Giá nhập", "Giá bán", "Hạn sử dụng", "Trạng thái");

        for (var i = 0; i < products.Count; i++)
        {
            var row = i + 2;
            var p = products[i];
            ws.Cell(row, 1).Value = p.Barcode ?? string.Empty;
            ws.Cell(row, 2).Value = p.ProductName;
            ws.Cell(row, 3).Value = p.CategoryName ?? string.Empty;
            ws.Cell(row, 4).Value = p.Unit ?? string.Empty;
            ws.Cell(row, 5).Value = p.StockQuantity;
            ws.Cell(row, 6).Value = p.MinStock;
            ws.Cell(row, 7).Value = p.CostPrice;
            ws.Cell(row, 8).Value = p.SellingPrice;
            ws.Cell(row, 9).Value = p.ExpiryDate.HasValue ? p.ExpiryDate.Value.ToString("dd/MM/yyyy") : string.Empty;

            string status;
            Color cellColor;
            if (p.StockQuantity <= 0)
            {
                status = "Hết hàng";
                cellColor = Color.FromArgb(255, 220, 220);
            }
            else if (p.StockQuantity <= p.MinStock)
            {
                status = "Sắp hết";
                cellColor = Color.FromArgb(255, 243, 205);
            }
            else
            {
                status = "Còn hàng";
                cellColor = Color.FromArgb(220, 255, 220);
            }

            ws.Cell(row, 10).Value = status;
            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromColor(cellColor);
        }

        FormatWorksheet(ws, products.Count);
        workbook.SaveAs(filePath);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    private static void SetHeader(IXLWorksheet ws, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }
    }

    private static void SetHeaderAt(IXLWorksheet ws, int rowNum, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(rowNum, i + 1).Value = headers[i];
            ws.Cell(rowNum, i + 1).Style.Font.Bold = true;
        }
    }

    private static void FormatWorksheet(IXLWorksheet ws, int dataCount)
    {
        ws.Columns().AdjustToContents();
        if (dataCount > 0)
        {
            ws.Range(1, 1, dataCount + 1, ws.LastColumnUsed()?.ColumnNumber() ?? 1)
              .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, dataCount + 1, ws.LastColumnUsed()?.ColumnNumber() ?? 1)
              .Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
    }

    public void ExportSalesToCsv(string filePath, List<SaleHistoryItem> sales)
    {
        var lines = new List<string> { "SaleID,SaleDate,CustomerName,TotalAmount,IsDebt" };

        foreach (var sale in sales)
        {
            lines.Add(string.Join(",",
                sale.SaleID,
                EscapeCsv(sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(sale.CustomerName ?? string.Empty),
                sale.TotalAmount.ToString("0.##"),
                sale.IsDebt ? "1" : "0"));
        }

        WriteUtf8Bom(filePath, lines);
    }

    public void ExportOutstandingDebtsToCsv(string filePath, List<DebtSaleItem> debts)
    {
        var lines = new List<string> { "SaleID,SaleDate,CustomerName,Phone,TotalAmount,PaidAmount,OutstandingAmount" };

        foreach (var debt in debts)
        {
            lines.Add(string.Join(",",
                debt.SaleID,
                EscapeCsv(debt.SaleDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(debt.CustomerName ?? string.Empty),
                EscapeCsv(debt.CustomerPhone ?? string.Empty),
                debt.TotalAmount.ToString("0.##"),
                debt.PaidAmount.ToString("0.##"),
                debt.OutstandingAmount.ToString("0.##")));
        }

        WriteUtf8Bom(filePath, lines);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static void WriteUtf8Bom(string filePath, IEnumerable<string> lines)
    {
        var content = string.Join(Environment.NewLine, lines);
        var utf8WithBom = new UTF8Encoding(true);
        File.WriteAllText(filePath, content, utf8WithBom);
    }
}
