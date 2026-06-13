using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Catalog;

public sealed class CatalogIndexViewModel
{
    public required ProductQuery Query { get; init; }

    public required IReadOnlyList<ProductCardViewModel> Products { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage { get; init; }

    public bool HasNextPage { get; init; }
}
