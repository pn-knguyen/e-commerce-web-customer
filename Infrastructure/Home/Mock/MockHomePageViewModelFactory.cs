using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.ViewModels.Home;

namespace e_commerce_web_customer.Infrastructure.Home.Mock;

public sealed class MockHomePageViewModelFactory : IHomePageViewModelFactory
{
    private readonly ISiteCategoryMenuProvider _categoryMenuProvider;

    public MockHomePageViewModelFactory(ISiteCategoryMenuProvider categoryMenuProvider)
    {
        _categoryMenuProvider = categoryMenuProvider;
    }

    public Task<HomeIndexViewModel> CreateAsync()
    {
        return Task.FromResult(new HomeIndexViewModel
        {
            Hero = HomeHeroViewModelFactory.Create(_categoryMenuProvider.GetMenu().Items),
            FeaturedCategorySections = [PhoneCategorySectionFactory.Create()],
            AccessoryDirectory = AccessoryDirectoryFactory.Create(),
            AdditionalCategorySections =
            [
                ComputerCategorySectionFactory.Create(),
                AudioWearablesCategorySectionFactory.Create()
            ]
        });
    }
}
