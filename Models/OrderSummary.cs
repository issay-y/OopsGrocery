namespace OopsGrocery.Models;

public sealed class OrderSummary
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Processing";
    public string PlacedAt { get; set; } = string.Empty;

    public string FormattedTotal => $"${TotalAmount:F2}";
}
