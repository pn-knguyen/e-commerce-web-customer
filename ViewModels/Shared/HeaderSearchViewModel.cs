namespace e_commerce_web_customer.ViewModels.Shared;

public sealed class HeaderSearchViewModel
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<SearchQuickLinkViewModel> RecentSearches { get; init; } = [];
    public IReadOnlyList<SearchQuickLinkViewModel> TrendingSearches { get; init; } = [];
    public string EmptyLogoUrl { get; init; } = "/images/logo-techstore-icon.svg";
}

public sealed class SearchSuggestionResultsViewModel
{
    public required string Query { get; init; }
    public IReadOnlyList<SearchQuickLinkViewModel> Suggestions { get; init; } = [];
    public IReadOnlyList<SearchProductSuggestionViewModel> Products { get; init; } = [];
    public bool HasResults => Suggestions.Count > 0 || Products.Count > 0;
}

public sealed class SearchQuickLinkViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAlt { get; init; }
}

public sealed class SearchProductSuggestionViewModel
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public required string PriceText { get; init; }
    public string? OldPriceText { get; init; }
}
