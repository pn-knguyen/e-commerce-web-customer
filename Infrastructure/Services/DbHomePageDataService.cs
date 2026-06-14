using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbHomePageDataService(IProductCatalog productCatalog)
    : IHomePageDataService
{
    public async Task<HomeIndexViewModel> CreateHomePageAsync(
        SiteCategoryMenuViewModel categoryMenu,
        CancellationToken cancellationToken = default)
    {
        var products = await productCatalog.SearchAsync(null, cancellationToken);
        var productCards = products
            .Take(20)
            .Select(ProductViewModelMapper.ToProductCard)
            .ToList();

        return new HomeIndexViewModel
        {
            Hero = HomeHeroViewModelFactory.Create(categoryMenu.Items),
            FeaturedCategorySections =
            [
                PhoneCategorySectionFactory.Create(productCards)
            ],
            AccessoryDirectory = AccessoryDirectoryFactory.Create(),
            AdditionalCategorySections =
            [
                ComputerCategorySectionFactory.Create(),
                AudioWearablesCategorySectionFactory.Create()
            ]
        };
    }
}
