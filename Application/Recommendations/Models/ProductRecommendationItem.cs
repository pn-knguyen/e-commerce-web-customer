namespace e_commerce_web_customer.Application.Recommendations.Models;

public sealed class ProductRecommendationItem
{
    public required string ProductVariantKey { get; init; }
    public string ProductSlug { get; init; } = string.Empty;
    public required string Url { get; init; }
    public required string Name { get; init; }
    public string VariantLabel { get; init; } = string.Empty;
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public required string MemberOffer { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public IReadOnlyList<ProductRecommendationVariantItem> Variants { get; init; } = [];
    public int Score { get; init; }
    public required string Source { get; init; }
}
