namespace e_commerce_web_customer.Application.Products;

public sealed record ProductReadModel(
    string Id,
    string Name,
    string ProductUrl,
    string ImageUrl,
    string ImageAlt,
    decimal CurrentPrice,
    decimal? OldPrice,
    int DiscountPercent,
    decimal? StudentPrice,
    string? PromotionNote,
    string SearchText,
    IReadOnlyList<string>? Aliases = null);
