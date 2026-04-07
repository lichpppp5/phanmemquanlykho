using PMTapHoa.Desktop.Models;
using System.Drawing.Printing;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class ReceiptService
{
    public string GenerateTempReceiptFile(int saleId, List<CartItem> items, decimal total, string? customerName, bool isDebt)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"hoadon_{saleId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var content = BuildReceiptText(saleId, items, total, customerName, isDebt, 58);
        File.WriteAllText(tempFile, content, Encoding.UTF8);
        return tempFile;
    }

    public void PrintThermalReceipt(
        int saleId,
        List<CartItem> items,
        decimal total,
        string? customerName,
        bool isDebt,
        int paperWidthMm,
        string? printerName = null)
    {
        var width = paperWidthMm == 80 ? 80 : 58;
        var text = BuildReceiptText(saleId, items, total, customerName, isDebt, width);
        PrintText(text, width, printerName);
    }

    private static string BuildReceiptText(
        int saleId,
        List<CartItem> items,
        decimal total,
        string? customerName,
        bool isDebt,
        int paperWidthMm)
    {
        var charsPerLine = paperWidthMm == 80 ? 42 : 30;
        var nameWidth = paperWidthMm == 80 ? 24 : 16;
        var builder = new StringBuilder();
        builder.AppendLine("===== CUA HANG TAP HOA =====");
        builder.AppendLine($"Hoa don: #{saleId}");
        builder.AppendLine($"Ngay: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        builder.AppendLine($"Khach: {customerName ?? "Khach le"}");
        builder.AppendLine(new string('-', charsPerLine));
        builder.AppendLine(paperWidthMm == 80 ? "Ten hang                 SL         Gia" : "Ten hang          SL     Gia");

        foreach (var item in items)
        {
            var name = Shorten(item.ProductName, nameWidth).PadRight(nameWidth);
            builder.AppendLine($"{name} {item.Quantity,3} {item.LineTotal,10:N0}");
        }

        builder.AppendLine(new string('-', charsPerLine));
        builder.AppendLine($"Tong tien: {total:N0} VND");
        builder.AppendLine($"Trang thai: {(isDebt ? "Ghi no" : "Da thanh toan")}");
        builder.AppendLine("Cam on quy khach!");
        return builder.ToString();
    }

    private static void PrintText(string text, int paperWidthMm, string? printerName)
    {
        using var printDocument = new PrintDocument();
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            printDocument.PrinterSettings.PrinterName = printerName.Trim();
        }

        if (!printDocument.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException("Không tìm thấy máy in hợp lệ.");
        }

        var widthInHundredthInch = (int)Math.Round(paperWidthMm / 25.4 * 100);
        var paperSize = new PaperSize("Thermal", widthInHundredthInch, 1200);
        printDocument.DefaultPageSettings.PaperSize = paperSize;
        printDocument.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

        var lines = text.Split(Environment.NewLine);
        var currentLine = 0;
        var font = new Font("Consolas", paperWidthMm == 80 ? 9 : 8, FontStyle.Regular);

        printDocument.PrintPage += (_, e) =>
        {
            var lineHeight = font.GetHeight(e.Graphics) + 1;
            float y = e.MarginBounds.Top;
            while (currentLine < lines.Length)
            {
                if (y + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                e.Graphics.DrawString(lines[currentLine], font, Brushes.Black, e.MarginBounds.Left, y);
                y += lineHeight;
                currentLine++;
            }

            e.HasMorePages = false;
        };

        printDocument.PrintController = new StandardPrintController();
        printDocument.Print();
    }

    private static string Shorten(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }
}
