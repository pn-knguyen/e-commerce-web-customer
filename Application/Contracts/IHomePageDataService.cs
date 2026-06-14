using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Contracts;

public interface IHomePageDataService
{
    Task<HomeIndexViewModel> CreateHomePageAsync(
        SiteCategoryMenuViewModel categoryMenu,
        CancellationToken cancellationToken = default);
}
