namespace PMTapHoa.Core.Services;

/// <summary>Tính toán giảm giá theo phần trăm hoặc số tiền cố định.</summary>
public class DiscountService
{
    public enum DiscountType { None, Percent, FixedAmount }

    public record DiscountResult(decimal DiscountAmount, string Note);

    public static DiscountResult Calculate(decimal subtotal, DiscountType type, decimal value)
    {
        return type switch
        {
            DiscountType.Percent => new DiscountResult(
                Math.Round(subtotal * value / 100, 0),
                $"Giảm {value:0.##}%"),
            DiscountType.FixedAmount => new DiscountResult(
                Math.Min(value, subtotal),
                $"Giảm {value:N0} VND"),
            _ => new DiscountResult(0, string.Empty)
        };
    }

    public static decimal FinalAmount(decimal subtotal, decimal discountAmount)
    {
        return Math.Max(0, subtotal - discountAmount);
    }
}
