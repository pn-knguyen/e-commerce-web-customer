namespace e_commerce_web_customer.Application.Recommendations.Models;

public enum RecommendationSurface
{
    ProductDetailAccessoryUpsell = 1
}

public sealed class ProductRecommendationRequest
{
    public long ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string ProductSlug { get; init; }
    public string? BrandSlug { get; init; }
    public string? CategorySlug { get; init; }
    public RecommendationSurface Surface { get; init; } = RecommendationSurface.ProductDetailAccessoryUpsell;
    public int Limit { get; init; } = 6;
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
}
