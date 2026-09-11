using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class InventoryService
{
    public List<Product> GetLowStockProducts(List<Product> products)
    {
        return products.Where(p => p.StockQuantity < p.MinStock).ToList();
    }

    public List<Product> GetNearExpiryProducts(List<Product> products, int days = 14)
    {
        var threshold = DateTime.Today.AddDays(days);
        return products
            .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value.Date <= threshold)
            .ToList();
    }
}
