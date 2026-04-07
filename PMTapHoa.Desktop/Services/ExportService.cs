using ClosedXML.Excel;
using PMTapHoa.Desktop.Models;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class ExportService
{
    public void ExportSalesToExcel(string filePath, List<SaleHistoryItem> sales)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sales");
        ws.Cell(1, 1).Value = "SaleID";
        ws.Cell(1, 2).Value = "SaleDate";
        ws.Cell(1, 3).Value = "CustomerName";
        ws.Cell(1, 4).Value = "TotalAmount";
        ws.Cell(1, 5).Value = "IsDebt";

        for (var i = 0; i < sales.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = sales[i].SaleID;
            ws.Cell(row, 2).Value = sales[i].SaleDate;
            ws.Cell(row, 3).Value = sales[i].CustomerName ?? string.Empty;
            ws.Cell(row, 4).Value = sales[i].TotalAmount;
            ws.Cell(row, 5).Value = sales[i].IsDebt ? "Yes" : "No";
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportOutstandingDebtsToExcel(string filePath, List<DebtSaleItem> debts)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Debts");
        ws.Cell(1, 1).Value = "SaleID";
        ws.Cell(1, 2).Value = "SaleDate";
        ws.Cell(1, 3).Value = "CustomerName";
        ws.Cell(1, 4).Value = "TotalAmount";
        ws.Cell(1, 5).Value = "PaidAmount";
        ws.Cell(1, 6).Value = "OutstandingAmount";

        for (var i = 0; i < debts.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = debts[i].SaleID;
            ws.Cell(row, 2).Value = debts[i].SaleDate;
            ws.Cell(row, 3).Value = debts[i].CustomerName ?? string.Empty;
            ws.Cell(row, 4).Value = debts[i].TotalAmount;
            ws.Cell(row, 5).Value = debts[i].PaidAmount;
            ws.Cell(row, 6).Value = debts[i].OutstandingAmount;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportSalesToCsv(string filePath, List<SaleHistoryItem> sales)
    {
        var lines = new List<string>
        {
            "SaleID,SaleDate,CustomerName,TotalAmount,IsDebt"
        };

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
        var lines = new List<string>
        {
            "SaleID,SaleDate,CustomerName,TotalAmount,PaidAmount,OutstandingAmount"
        };

        foreach (var debt in debts)
        {
            lines.Add(string.Join(",",
                debt.SaleID,
                EscapeCsv(debt.SaleDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(debt.CustomerName ?? string.Empty),
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
