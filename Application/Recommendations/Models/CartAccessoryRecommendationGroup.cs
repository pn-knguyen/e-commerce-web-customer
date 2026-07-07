namespace e_commerce_web_customer.Application.Recommendations.Models;

public sealed class CartAccessoryRecommendationGroup
{
    public required string ParentProductVariantKey { get; init; }
    public required string ParentProductName { get; init; }
    public string ParentVariantLabel { get; init; } = string.Empty;
    public IReadOnlyList<ProductRecommendationItem> Items { get; init; } = [];
}
