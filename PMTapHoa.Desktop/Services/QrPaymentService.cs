using QRCoder;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class QrPaymentService
{
    private readonly AppConfigService _config;

    public QrPaymentService(AppConfigService config)
    {
        _config = config;
    }

    public bool IsConfigured(out string message)
    {
        if (!_config.QrPaymentEnabled)
        {
            message = "Tính năng QR đang tắt trong cấu hình.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_config.QrBankBin) || string.IsNullOrWhiteSpace(_config.QrAccountNo))
        {
            message = "Thiếu thông tin ngân hàng hoặc số tài khoản nhận tiền.";
            return false;
        }

        message = "OK";
        return true;
    }

    public string BuildTransferContent()
    {
        var prefix = (_config.QrTransferPrefix ?? "HD").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "HD";
        }

        return $"{prefix}{DateTime.Now:yyyyMMddHHmmss}";
    }

    public Bitmap GenerateVietQrImage(decimal amount, string transferContent)
    {
        var payload = BuildVietQrPayload(
            _config.QrBankBin,
            _config.QrAccountNo,
            _config.QrAccountName,
            amount,
            transferContent);

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrData);
        return qrCode.GetGraphic(20, Color.Black, Color.White, true);
    }

    private static string BuildVietQrPayload(
        string bankBin,
        string accountNo,
        string accountName,
        decimal amount,
        string transferContent)
    {
        var accountInfo = BuildTlv("00", "A000000727")
                        + BuildTlv("01", BuildTlv("00", bankBin.Trim()) + BuildTlv("01", accountNo.Trim()))
                        + BuildTlv("02", "QRIBFTTA");

        var payload = new StringBuilder();
        payload.Append(BuildTlv("00", "01")); // Payload format indicator
        payload.Append(BuildTlv("01", "12")); // Dynamic QR
        payload.Append(BuildTlv("38", accountInfo)); // Merchant account info
        payload.Append(BuildTlv("53", "704")); // Currency VND

        if (amount > 0)
        {
            payload.Append(BuildTlv("54", amount.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        payload.Append(BuildTlv("58", "VN")); // Country
        payload.Append(BuildTlv("59", RemoveVietnameseDiacritics(accountName).ToUpperInvariant()));
        payload.Append(BuildTlv("60", "HOCHIMINH"));
        payload.Append(BuildTlv("62", BuildTlv("08", transferContent.Trim())));

        var withoutCrc = payload + "6304";
        var crc = ComputeCrc16Ccitt(withoutCrc);
        return withoutCrc + crc;
    }

    private static string BuildTlv(string tag, string value)
    {
        var safe = value ?? string.Empty;
        return $"{tag}{safe.Length:00}{safe}";
    }

    private static string ComputeCrc16Ccitt(string input)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;
        var bytes = Encoding.ASCII.GetBytes(input);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ polynomial)
                    : (ushort)(crc << 1);
            }
        }

        return crc.ToString("X4");
    }

    private static string RemoveVietnameseDiacritics(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "CUA HANG TAP HOA";
        }

        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
