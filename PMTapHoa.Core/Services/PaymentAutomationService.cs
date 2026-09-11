using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public enum PaymentStatus
{
    Pending,
    Success,
    Expired,
    Cancelled
}

public class PendingPayment
{
    public string PaymentCode { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; } = DateTime.Now.AddMinutes(15);
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal? ReceivedAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? BankName { get; set; }
    public string? TransactionRef { get; set; }
    public string? Source { get; set; }
    public string? RawContent { get; set; }
}

public class PaymentLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public string SourceType { get; set; } = "Webhook"; // SePay, Casso, PhoneNotification, Simulation
    public string? BankName { get; set; }
    public decimal Amount { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string? MatchedCode { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PaymentAutomationService
{
    private readonly AppConfigService _config;
    private readonly ConcurrentDictionary<string, PendingPayment> _pendingPayments = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<PaymentLogEntry> _recentLogs = new();
    private const int MaxLogs = 50;

    public PaymentAutomationService(AppConfigService config)
    {
        _config = config;
    }

    /// <summary>
    /// Đăng ký giao dịch thanh toán chờ khách quét VietQR
    /// </summary>
    public PendingPayment CreatePendingPayment(string paymentCode, decimal amount, string? customerName = null)
    {
        paymentCode = paymentCode.Trim().ToUpperInvariant();
        var pending = new PendingPayment
        {
            PaymentCode = paymentCode,
            ExpectedAmount = amount,
            CustomerName = customerName ?? "Khách lẻ",
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(15),
            Status = PaymentStatus.Pending
        };

        _pendingPayments[paymentCode] = pending;
        CleanupExpiredPayments();
        return pending;
    }

    /// <summary>
    /// Lấy trạng thái giao dịch thanh toán theo mã
    /// </summary>
    public PendingPayment? GetPayment(string paymentCode)
    {
        if (string.IsNullOrWhiteSpace(paymentCode)) return null;
        paymentCode = paymentCode.Trim().ToUpperInvariant();

        if (_pendingPayments.TryGetValue(paymentCode, out var payment))
        {
            if (payment.Status == PaymentStatus.Pending && DateTime.Now > payment.ExpiresAt)
            {
                payment.Status = PaymentStatus.Expired;
            }
            return payment;
        }

        return null;
    }

    /// <summary>
    /// Lấy danh sách nhật ký giao dịch nhận được gần đây
    /// </summary>
    public List<PaymentLogEntry> GetRecentLogs()
    {
        return _recentLogs.Reverse().Take(MaxLogs).ToList();
    }

    /// <summary>
    /// GIẢI PHÁP 1: Xử lý Webhook từ SePay / Casso hoặc Cổng thanh toán ngân hàng
    /// </summary>
    public (bool Success, string Message, PendingPayment? Matched) ProcessWebhook(string rawJson, string? secretHeader = null)
    {
        var log = new PaymentLogEntry
        {
            SourceType = "Webhook (SePay/Casso)",
            RawContent = rawJson
        };

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // 1. Kiểm tra định dạng SePay
            // SePay payload: { id, gateway, transactionDate, accountNumber, transferType: "in", transferAmount, content, referenceCode }
            if (root.TryGetProperty("transferAmount", out var sepayAmountProp) ||
                (root.TryGetProperty("gateway", out _) && root.TryGetProperty("content", out _)))
            {
                var amount = sepayAmountProp.ValueKind == JsonValueKind.Number
                    ? sepayAmountProp.GetDecimal()
                    : ParseDecimalSafe(sepayAmountProp.GetString());

                var content = root.TryGetProperty("content", out var cProp) ? cProp.GetString() ?? "" : "";
                var gateway = root.TryGetProperty("gateway", out var gProp) ? gProp.GetString() : "SePay Gateway";
                var refCode = root.TryGetProperty("referenceCode", out var rProp) ? rProp.GetString() : "";

                log.BankName = gateway;
                log.Amount = amount;
                log.SourceType = "SePay Webhook";

                return ReconcilePayment(content, amount, gateway, refCode, "SePay Webhook", log);
            }

            // 2. Kiểm tra định dạng Casso
            // Casso payload: { error: 0, data: [ { id, tid, description, amount, cusum_balance, bank_sub_acc_id } ] }
            if (root.TryGetProperty("data", out var cassoDataProp) && cassoDataProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in cassoDataProp.EnumerateArray())
                {
                    var amount = item.TryGetProperty("amount", out var aProp) && aProp.ValueKind == JsonValueKind.Number
                        ? aProp.GetDecimal()
                        : 0;
                    var desc = item.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? "" : "";
                    var tid = item.TryGetProperty("tid", out var tProp) ? tProp.GetString() : "";

                    log.BankName = "Casso Gateway";
                    log.Amount = amount;
                    log.SourceType = "Casso Webhook";

                    var res = ReconcilePayment(desc, amount, "Casso", tid, "Casso Webhook", log);
                    if (res.Success) return res;
                }
            }

            // 3. Kiểm tra định dạng tuỳ chỉnh đơn giản (content, amount, bank)
            if (root.TryGetProperty("amount", out var genAmountProp))
            {
                var amount = genAmountProp.ValueKind == JsonValueKind.Number
                    ? genAmountProp.GetDecimal()
                    : ParseDecimalSafe(genAmountProp.GetString());
                var content = root.TryGetProperty("content", out var cProp) ? cProp.GetString() ?? "" :
                              root.TryGetProperty("text", out var tProp) ? tProp.GetString() ?? "" : "";
                var bank = root.TryGetProperty("bank", out var bProp) ? bProp.GetString() : "Custom Bank";

                log.BankName = bank;
                log.Amount = amount;
                log.SourceType = "Custom Webhook";

                return ReconcilePayment(content, amount, bank, null, "Custom Webhook", log);
            }

            log.IsSuccess = false;
            log.Message = "Không nhận diện được định dạng payload Webhook (hỗ trợ SePay, Casso, Custom JSON).";
            AddLog(log);
            return (false, log.Message, null);
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.Message = $"Lỗi phân tích JSON Webhook: {ex.Message}";
            AddLog(log);
            return (false, log.Message, null);
        }
    }

    /// <summary>
    /// GIẢI PHÁP 2: Xử lý thông báo biến động số dư hoặc tin nhắn SMS từ ứng dụng điện thoại (MacroDroid / SMS Forwarder)
    /// </summary>
    public (bool Success, string Message, PendingPayment? Matched) ProcessPhoneNotification(string text, string? senderOrBank = null)
    {
        var log = new PaymentLogEntry
        {
            SourceType = "Phone SMS / Notification",
            RawContent = text,
            BankName = senderOrBank ?? DetectBankFromText(text)
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            log.IsSuccess = false;
            log.Message = "Nội dung tin nhắn / thông báo trống.";
            AddLog(log);
            return (false, log.Message, null);
        }

        // Parse số tiền từ SMS/Notification
        var amount = ExtractAmountFromText(text);
        log.Amount = amount;

        if (amount <= 0)
        {
            log.IsSuccess = false;
            log.Message = "Không trích xuất được số tiền nhận (+VND) từ thông báo.";
            AddLog(log);
            return (false, log.Message, null);
        }

        var bankName = log.BankName ?? DetectBankFromText(text);
        return ReconcilePayment(text, amount, bankName, null, "Phone SMS / Notification", log);
    }

    /// <summary>
    /// Xác nhận thủ công khi nhân viên xác nhận khách đã chuyển khoản (sử dụng khi không có kết nối webhook).
    /// </summary>
    public (bool Success, string Message, PendingPayment? Matched) SimulateTransfer(string paymentCode, decimal? amount = null, string? bank = null)
    {
        paymentCode = paymentCode.Trim().ToUpperInvariant();
        if (_pendingPayments.TryGetValue(paymentCode, out var pending))
        {
            var transferAmount = amount ?? pending.ExpectedAmount;
            var log = new PaymentLogEntry
            {
                SourceType = "Xác nhận thủ công",
                BankName = bank ?? "Không xác định",
                Amount = transferAmount,
                RawContent = $"Xác nhận thủ công: {transferAmount:N0}đ cho đơn {paymentCode}",
                MatchedCode = paymentCode,
                IsSuccess = true,
                Message = $"Xác nhận thanh toán đơn {paymentCode} thành công!"
            };

            pending.Status = PaymentStatus.Success;
            pending.ReceivedAmount = transferAmount;
            pending.PaidAt = DateTime.Now;
            pending.BankName = bank ?? "Không xác định";
            pending.Source = "Xác nhận thủ công";
            pending.TransactionRef = "SIM" + DateTime.Now.ToString("yyyyMMddHHmmss");

            AddLog(log);
            return (true, log.Message, pending);
        }

        return (false, $"Không tìm thấy đơn hàng đang chờ thanh toán với mã: {paymentCode}", null);
    }

    /// <summary>
    /// Bóc tách mã đơn hàng và so khớp với danh sách giao dịch đang chờ
    /// </summary>
    private (bool Success, string Message, PendingPayment? Matched) ReconcilePayment(
        string content,
        decimal amount,
        string? bank,
        string? refCode,
        string source,
        PaymentLogEntry log)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            log.IsSuccess = false;
            log.Message = "Nội dung chuyển khoản rỗng, không tìm thấy mã đơn.";
            AddLog(log);
            return (false, log.Message, null);
        }

        // Tìm kiếm các mẫu mã đơn hàng phổ biến: HDxxxxxx hoặc TTxxxxxx hoặc bất kỳ mã nào trong danh sách pending
        var match = Regex.Match(content, @"(HD\d{4,8}|TT\d{4,8})", RegexOptions.IgnoreCase);
        string? detectedCode = null;

        if (match.Success)
        {
            detectedCode = match.Groups[1].Value.ToUpperInvariant();
        }
        else
        {
            // Kiểm tra xem có trùng với bất kỳ mã pending nào trong bộ nhớ không
            foreach (var key in _pendingPayments.Keys)
            {
                if (content.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    detectedCode = key;
                    break;
                }
            }
        }

        log.MatchedCode = detectedCode;

        if (string.IsNullOrEmpty(detectedCode))
        {
            log.IsSuccess = false;
            log.Message = $"Nhận được biến động số dư {amount:N0}đ nhưng không tìm thấy mã hoá đơn trong nội dung: '{content}'.";
            AddLog(log);
            return (false, log.Message, null);
        }

        if (!_pendingPayments.TryGetValue(detectedCode, out var pending))
        {
            log.IsSuccess = false;
            log.Message = $"Tìm thấy mã {detectedCode} ({amount:N0}đ) nhưng mã này không có trong danh sách đơn đang chờ hoặc đã hoàn thành trước đó.";
            AddLog(log);
            return (false, log.Message, null);
        }

        if (pending.Status == PaymentStatus.Success)
        {
            log.IsSuccess = true;
            log.Message = $"Đơn hàng {detectedCode} đã được ghi nhận thanh toán thành công trước đó.";
            AddLog(log);
            return (true, log.Message, pending);
        }

        // So khớp số tiền nhận được với số tiền cần thanh toán
        if (amount < pending.ExpectedAmount)
        {
            log.IsSuccess = false;
            log.Message = $"Số tiền chuyển ({amount:N0}đ) nhỏ hơn tổng hoá đơn ({pending.ExpectedAmount:N0}đ) cho mã {detectedCode}.";
            AddLog(log);
            return (false, log.Message, pending);
        }

        // Cập nhật thành công!
        pending.Status = PaymentStatus.Success;
        pending.ReceivedAmount = amount;
        pending.PaidAt = DateTime.Now;
        pending.BankName = bank ?? "Ngân hàng";
        pending.TransactionRef = refCode ?? ("REF" + DateTime.Now.ToString("yyyyMMddHHmmss"));
        pending.Source = source;
        pending.RawContent = content;

        log.IsSuccess = true;
        log.Message = $"✓ Khớp đơn {detectedCode} thành công! Đã nhận {amount:N0}đ qua {bank ?? "Ngân hàng"}.";
        AddLog(log);

        return (true, log.Message, pending);
    }

    /// <summary>
    /// Bộ parser trích xuất số tiền biến động từ tin nhắn SMS và Notification các ngân hàng Việt Nam
    /// </summary>
    private static decimal ExtractAmountFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        // Pattern 1: +150,000VND hoặc + 150.000 đ hoặc +150000d
        var m1 = Regex.Match(text, @"\+\s*([0-9]{1,3}(?:[,.][0-9]{3})+|[0-9]{4,10})\s*(?:VND|đ|d|dong)?", RegexOptions.IgnoreCase);
        if (m1.Success)
        {
            var raw = m1.Groups[1].Value.Replace(",", "").Replace(".", "");
            if (decimal.TryParse(raw, out var val)) return val;
        }

        // Pattern 2: tang 150.000 VND hoặc PS: +150,000
        var m2 = Regex.Match(text, @"(?:tang|nhan|so tien|ps:\s*\+)\s*([0-9]{1,3}(?:[,.][0-9]{3})+|[0-9]{4,10})\s*(?:VND|đ|d)?", RegexOptions.IgnoreCase);
        if (m2.Success)
        {
            var raw = m2.Groups[1].Value.Replace(",", "").Replace(".", "");
            if (decimal.TryParse(raw, out var val)) return val;
        }

        // Pattern 3: GD: +150,000
        var m3 = Regex.Match(text, @"GD:\s*\+?\s*([0-9]{1,3}(?:[,.][0-9]{3})+|[0-9]{4,10})", RegexOptions.IgnoreCase);
        if (m3.Success)
        {
            var raw = m3.Groups[1].Value.Replace(",", "").Replace(".", "");
            if (decimal.TryParse(raw, out var val)) return val;
        }

        return 0;
    }

    private static string DetectBankFromText(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("vcb") || lower.Contains("vietcombank")) return "Vietcombank";
        if (lower.Contains("mbbank") || lower.Contains("mb bank") || lower.Contains("mb")) return "MBBank";
        if (lower.Contains("techcombank") || lower.Contains("tcb")) return "Techcombank";
        if (lower.Contains("acb")) return "ACB";
        if (lower.Contains("bidv")) return "BIDV";
        if (lower.Contains("vpbank") || lower.Contains("vpb")) return "VPBank";
        if (lower.Contains("tpbank") || lower.Contains("tpb")) return "TPBank";
        if (lower.Contains("agribank")) return "Agribank";
        if (lower.Contains("sacombank") || lower.Contains("stb")) return "Sacombank";
        if (lower.Contains("vietinbank") || lower.Contains("ctg")) return "VietinBank";
        if (lower.Contains("ocb")) return "OCB";
        if (lower.Contains("hdbank")) return "HDBank";
        if (lower.Contains("seabank")) return "SeABank";
        if (lower.Contains("msb")) return "MSB";
        if (lower.Contains("vib")) return "VIB";
        if (lower.Contains("shb")) return "SHB";
        if (lower.Contains("timo")) return "Timo";
        if (lower.Contains("cake")) return "Cake by VPBank";
        return "Ngân hàng (SMS/Noti)";
    }

    private static decimal ParseDecimalSafe(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0;
        var clean = str.Replace(",", "").Replace(".", "").Trim();
        return decimal.TryParse(clean, out var val) ? val : 0;
    }

    private void AddLog(PaymentLogEntry log)
    {
        _recentLogs.Enqueue(log);
        while (_recentLogs.Count > MaxLogs && _recentLogs.TryDequeue(out _)) { }
    }

    private void CleanupExpiredPayments()
    {
        var now = DateTime.Now;
        var expiredKeys = _pendingPayments
            .Where(kvp => kvp.Value.Status == PaymentStatus.Expired || (kvp.Value.Status == PaymentStatus.Pending && kvp.Value.ExpiresAt < now.AddHours(-1)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var k in expiredKeys)
        {
            _pendingPayments.TryRemove(k, out _);
        }
    }
}
