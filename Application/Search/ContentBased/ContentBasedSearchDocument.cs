using e_commerce_web_customer.Application.Products;

namespace e_commerce_web_customer.Application.Search.ContentBased;

public sealed record ContentBasedSearchDocument(
    string Id,
    string Name,
    string SearchText,
    IReadOnlyList<string> Aliases,
    string? BrandName,
    string? BrandSlug,
    string? CategoryName,
    string? CategorySlug,
    IReadOnlyList<string> Specifications,
    decimal CurrentPrice,
    int PopularityScore,
    decimal? Rating,
    bool IsAvailable)
{
    public static ContentBasedSearchDocument FromProduct(ProductReadModel product)
    {
        return new ContentBasedSearchDocument(
            product.Id,
            product.Name,
            product.SearchText,
            product.Aliases ?? [],
            product.BrandName,
            product.BrandSlug,
            product.CategoryName,
            product.CategorySlug,
            product.Specifications ?? [],
            product.CurrentPrice,
            product.PopularityScore,
            product.Rating,
            string.IsNullOrWhiteSpace(product.AvailabilityLabel));
    }
}
