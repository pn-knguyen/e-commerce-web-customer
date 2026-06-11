using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Home;

public sealed class CategoryProductsViewModel
{
    public required string Id { get; init; }
    public required IReadOnlyList<CategoryTabViewModel> Tabs { get; init; }
    public int Rows { get; init; } = 2;
    public bool EnableTabSwitching { get; init; }
    public bool ShowPagination { get; init; } = true;
}

public sealed class CategoryTabViewModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Url { get; init; }
    public bool IsActive { get; init; }
    public CategoryProductPanelViewModel? Panel { get; init; }
}

public sealed class CategoryProductPanelViewModel
{
    public IReadOnlyList<CategoryProductBannerViewModel> Banners { get; init; } = [];
    public required IReadOnlyList<CategoryQuickLinkViewModel> QuickLinks { get; init; }
    public required IReadOnlyList<CategoryBrandViewModel> Brands { get; init; }
    public required IReadOnlyList<ProductCardViewModel> Products { get; init; }
    public required string ViewAllUrl { get; init; }
}

public sealed class CategoryProductBannerViewModel
{
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public required string Url { get; init; }
}

public sealed class CategoryQuickLinkViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public required string ImageUrl { get; init; }
}

public sealed class CategoryBrandViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
}
