namespace e_commerce_web_customer.ViewModels.Cart;

public sealed class CartAccessoryRecommendationGroupViewModel
{
    public required string ParentProductVariantKey { get; init; }
    public required string ParentProductName { get; init; }
    public string ParentVariantLabel { get; init; } = string.Empty;
    public IReadOnlyList<CartAccessoryRecommendationViewModel> Items { get; init; } = [];
}
