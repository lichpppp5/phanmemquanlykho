using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public record NotificationSummary(
    int LowStockCount,
    int NearExpiryCount,
    int OverdueDebtCount,
    List<Product> LowStockProducts,
    List<Product> NearExpiryProducts);

public class NotificationService
{
    private readonly InventoryService _inventoryService;
    private readonly ProductService _productService;
    private readonly DebtService _debtService;

    public NotificationService(
        InventoryService inventoryService,
        ProductService productService,
        DebtService debtService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _debtService = debtService;
    }

    /// <summary>Lấy tổng hợp cảnh báo để hiển thị trên MainForm.</summary>
    public NotificationSummary GetSummary(int nearExpiryDays = 14)
    {
        var products = _productService.GetAll();
        var lowStock = _inventoryService.GetLowStockProducts(products);
        var nearExpiry = _inventoryService.GetNearExpiryProducts(products, nearExpiryDays);
        var overdueDebt = _debtService.GetOverdueDebtCount(30);

        return new NotificationSummary(
            lowStock.Count,
            nearExpiry.Count,
            overdueDebt,
            lowStock,
            nearExpiry);
    }

    public string BuildAlertText(NotificationSummary summary)
    {
        var parts = new List<string>();
        if (summary.LowStockCount > 0)
            parts.Add($"⚠ {summary.LowStockCount} mặt hàng sắp hết");
        if (summary.NearExpiryCount > 0)
            parts.Add($"📅 {summary.NearExpiryCount} mặt hàng gần hết hạn");
        if (summary.OverdueDebtCount > 0)
            parts.Add($"💰 {summary.OverdueDebtCount} khoản nợ quá 30 ngày");
        return parts.Count > 0 ? string.Join("   |   ", parts) : string.Empty;
    }
}
