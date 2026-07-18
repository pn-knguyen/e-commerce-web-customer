using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Search;

public sealed class SearchResultPageViewModel
{
    public required string Query { get; init; }
    public required int TotalCount { get; init; }
    public int InitialProductCount { get; init; } = 25;
    public int CurrentPage { get; init; } = 1;
    public bool HasMoreProducts { get; init; }
    public required IReadOnlyList<SearchResultCategoryViewModel> Categories { get; init; }
    public required IReadOnlyList<SearchResultSortOptionViewModel> SortOptions { get; init; }
    public required IReadOnlyList<ProductCardViewModel> Products { get; init; }

    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
    public bool HasProducts => Products.Count > 0;
    public int RemainingProductCount => Math.Max(0, TotalCount - CurrentPage * InitialProductCount);
}

public sealed class SearchResultCategoryViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public bool IsActive { get; init; }
}

public sealed class SearchResultSortOptionViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public required string Value { get; init; }
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
}
