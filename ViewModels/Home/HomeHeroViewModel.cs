using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Home;

public sealed class HomeHeroViewModel
{
    public required IReadOnlyList<SiteCategoryMenuItemViewModel> Categories { get; init; }
    public required IReadOnlyList<HomeHeroCampaignTabViewModel> CampaignTabs { get; init; }
    public required IReadOnlyList<HomeHeroSlideViewModel> Slides { get; init; }
    public required IReadOnlyList<HomeHeroPromoTileViewModel> PromoTiles { get; init; }
    public required IReadOnlyList<HomeHeroBenefitGroupViewModel> BenefitGroups { get; init; }
}

public sealed class HomeHeroCampaignTabViewModel
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Icon { get; init; }
    public bool IsActive { get; init; }
}

public sealed class HomeHeroSlideViewModel
{
    public required string Theme { get; init; }
    public required string BrandLabel { get; init; }
    public required string Kicker { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string PriceLabel { get; init; }
    public required string Price { get; init; }
    public required string ActionLabel { get; init; }
    public required string ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAlt { get; init; }
    public string? FeatureIcon { get; init; }
    public bool IsPriority { get; init; }
    public IReadOnlyList<HomeHeroServiceViewModel> Services { get; init; } = [];
}

public sealed class HomeHeroServiceViewModel
{
    public required string Icon { get; init; }
    public required string Label { get; init; }
    public required string StrongText { get; init; }
}

public sealed class HomeHeroPromoTileViewModel
{
    public required string Url { get; init; }
    public required string Eyebrow { get; init; }
    public required string Title { get; init; }
    public required string Price { get; init; }
    public required string Tone { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageAlt { get; init; }
    public string? FallbackIcon { get; init; }
}

public sealed class HomeHeroBenefitGroupViewModel
{
    public required string Title { get; init; }
    public required IReadOnlyList<HomeHeroBenefitViewModel> Items { get; init; }
}

public sealed class HomeHeroBenefitViewModel
{
    public required string Url { get; init; }
    public required string Label { get; init; }
    public required string StrongText { get; init; }
    public required string Icon { get; init; }
}
