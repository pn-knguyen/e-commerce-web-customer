using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Home;

public sealed class HomeApplianceShowcaseViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string ViewAllUrl { get; init; }
    public IReadOnlyList<CategoryDirectoryLinkViewModel> HeaderLinks { get; init; } = [];
    public required IReadOnlyList<HomeApplianceShowcaseColumnViewModel> Columns { get; init; }
}

public sealed class HomeApplianceShowcaseColumnViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string ViewAllUrl { get; init; }
    public required string BannerUrl { get; init; }
    public required string BannerImageUrl { get; init; }
    public required string BannerImageAlt { get; init; }
    public required IReadOnlyList<CategoryDirectoryItemViewModel> Items { get; init; }
}
