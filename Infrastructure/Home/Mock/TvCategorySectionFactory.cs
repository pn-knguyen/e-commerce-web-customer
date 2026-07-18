using e_commerce_web_customer.Infrastructure.Home.Content;
using e_commerce_web_customer.ViewModels.Home;

namespace e_commerce_web_customer.Infrastructure.Home.Mock;

internal static class TvCategorySectionFactory
{
    public static CategoryProductsViewModel Create()
    {
        return new CategoryProductsViewModel
        {
            Id = "tv-products",
            Rows = 1,
            EnableTabSwitching = false,
            ShowPagination = false,
            Tabs =
            [
                new CategoryTabViewModel
                {
                    Id = "tv",
                    Label = "TIVI",
                    Url = "/catalog?cat=tv",
                    IsActive = true,
                    Panel = new CategoryProductPanelViewModel
                    {
                        ViewAllUrl = "/catalog?cat=tv",
                        QuickLinks = HomeTvCategorySectionContent.CreateQuickLinks(),
                        Brands = HomeTvCategorySectionContent.CreateBrands(),
                        Products = HomeTvCategorySectionContent.CreateProducts()
                    }
                }
            ]
        };
    }
}
