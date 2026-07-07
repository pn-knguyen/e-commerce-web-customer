namespace e_commerce_web_customer.ViewModels.Cart;

public sealed class CartAccessoryRecommendationViewModel
{
    public required string ProductVariantKey { get; init; }
    public required string Url { get; init; }
    public required string Name { get; init; }
    public string VariantLabel { get; init; } = string.Empty;
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public required string MemberOffer { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal? OldPrice { get; init; }
}
