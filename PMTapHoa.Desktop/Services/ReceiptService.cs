using PMTapHoa.Desktop.Models;
using System.Drawing.Printing;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class ReceiptService
{
    public string GenerateTempReceiptFile(
        int saleId, List<CartItem> items, decimal total, string? customerName, bool isDebt,
        decimal discountAmount = 0, string? discountNote = null,
        string? storeName = null, string? storeAddress = null, string? storePhone = null, string? footer = null)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"hoadon_{saleId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var content = BuildReceiptText(saleId, items, total, customerName, isDebt, 58,
            discountAmount, discountNote, storeName, storeAddress, storePhone, footer);
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
        string? printerName = null,
        decimal discountAmount = 0,
        string? discountNote = null,
        string? storeName = null,
        string? storeAddress = null,
        string? storePhone = null,
        string? footer = null)
    {
        var width = paperWidthMm == 80 ? 80 : 58;
        var text = BuildReceiptText(saleId, items, total, customerName, isDebt, width,
            discountAmount, discountNote, storeName, storeAddress, storePhone, footer);
        PrintText(text, width, printerName);
    }

    private static string BuildReceiptText(
        int saleId,
        List<CartItem> items,
        decimal total,
        string? customerName,
        bool isDebt,
        int paperWidthMm,
        decimal discountAmount = 0,
        string? discountNote = null,
        string? storeName = null,
        string? storeAddress = null,
        string? storePhone = null,
        string? footer = null)
    {
        var charsPerLine = paperWidthMm == 80 ? 42 : 32;
        var nameWidth = paperWidthMm == 80 ? 24 : 16;
        var displayStoreName = string.IsNullOrWhiteSpace(storeName) ? "CUA HANG TAP HOA" : storeName.ToUpper();
        var displayFooter = string.IsNullOrWhiteSpace(footer) ? "Cam on quy khach! Hen gap lai." : footer;

        var builder = new StringBuilder();
        // Header
        builder.AppendLine(Center(displayStoreName, charsPerLine));
        if (!string.IsNullOrWhiteSpace(storeAddress))
            builder.AppendLine(Center(Shorten(storeAddress, charsPerLine), charsPerLine));
        if (!string.IsNullOrWhiteSpace(storePhone))
            builder.AppendLine(Center($"DT: {storePhone}", charsPerLine));
        builder.AppendLine(new string('=', charsPerLine));
        builder.AppendLine($"HOA DON: #{saleId:D6}");
        builder.AppendLine($"Ngay : {DateTime.Now:dd/MM/yyyy HH:mm}");
        builder.AppendLine($"Khach: {customerName ?? "Khach le"}");
        builder.AppendLine(new string('-', charsPerLine));

        // Items
        var header = paperWidthMm == 80
            ? "Ten hang                 SL      Thanh tien"
            : "Ten hang          SL    Tien";
        builder.AppendLine(header);
        builder.AppendLine(new string('-', charsPerLine));

        var subtotal = items.Sum(i => i.LineTotal);
        foreach (var item in items)
        {
            var name = Shorten(item.ProductName, nameWidth).PadRight(nameWidth);
            builder.AppendLine($"{name} {item.Quantity,3} {item.LineTotal,10:N0}");
            if (!string.IsNullOrWhiteSpace(item.Note))
                builder.AppendLine($"  -> {item.Note}");
        }

        builder.AppendLine(new string('-', charsPerLine));
        builder.AppendLine($"{"Tong cong:",-18} {subtotal,13:N0}");

        if (discountAmount > 0)
        {
            var discLabel = string.IsNullOrWhiteSpace(discountNote) ? "Giam gia:" : $"{discountNote}:";
            builder.AppendLine($"{Shorten(discLabel, 18),-18} {-discountAmount,13:N0}");
        }

        builder.AppendLine(new string('=', charsPerLine));
        builder.AppendLine($"{"THANH TOAN:",-18} {total,13:N0} VND");
        builder.AppendLine(new string('=', charsPerLine));
        builder.AppendLine($"Trang thai: {(isDebt ? "*** GHI NO ***" : "Da thanh toan")}");
        builder.AppendLine();
        builder.AppendLine(Center(displayFooter, charsPerLine));
        builder.AppendLine();
        return builder.ToString();
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        var padding = (width - text.Length) / 2;
        return text.PadLeft(text.Length + padding).PadRight(width);
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
            if (e.Graphics == null)
            {
                e.HasMorePages = false;
                return;
            }

            var graphics = e.Graphics;
            var lineHeight = font.GetHeight(graphics) + 1;
            float y = e.MarginBounds.Top;
            while (currentLine < lines.Length)
            {
                if (y + lineHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                graphics.DrawString(lines[currentLine], font, Brushes.Black, e.MarginBounds.Left, y);
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
