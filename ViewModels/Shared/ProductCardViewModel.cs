namespace e_commerce_web_customer.ViewModels.Shared;

public sealed class ProductCardViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public required string CurrentPrice { get; init; }
    public string? OldPrice { get; init; }
    public string? DiscountLabel { get; init; }
    public string? InstallmentLabel { get; init; } = "Trả góp 0%";
    public string? MemberOffer { get; init; }
    public string? StudentOffer { get; init; }
    public string? PromotionNote { get; init; }
    public string? DeliveryLabel { get; init; }
    public string? Location { get; init; }
    public decimal? Rating { get; init; }
    public int RatingCount { get; init; }
    public bool IsWishlisted { get; init; }
    public bool ShowWishlistAction { get; init; } = true;
}
