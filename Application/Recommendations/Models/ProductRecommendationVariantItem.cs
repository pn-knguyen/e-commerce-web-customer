namespace e_commerce_web_customer.Application.Recommendations.Models;

public sealed class ProductRecommendationVariantItem
{
    public required string ProductVariantKey { get; init; }
    public required string Url { get; init; }
    public required string Label { get; init; }
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public decimal CurrentPrice { get; init; }
    public int Quantity { get; init; }
    public bool IsDefault { get; init; }
}
