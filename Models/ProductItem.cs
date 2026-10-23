namespace OopsGrocery.Models;

public sealed class ProductItem
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = "ea";
    public int StockQuantity { get; set; }
    public string Tag { get; set; } = "Fresh";

    public bool IsLowStock => StockQuantity <= 10;
    public string FormattedPrice => $"${Price:F2}";
    public string FormattedPriceWithUnit => $"${Price:F2} / {Unit}";
}
