using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.ViewModels.Home;

public sealed class HomeIndexViewModel
{
    public required HomeHeroViewModel Hero { get; init; }
    public required IReadOnlyList<CategoryProductsViewModel> FeaturedCategorySections { get; init; }
    public required CategoryDirectoryViewModel AccessoryDirectory { get; init; }
    public required IReadOnlyList<CategoryProductsViewModel> AdditionalCategorySections { get; init; }
}
