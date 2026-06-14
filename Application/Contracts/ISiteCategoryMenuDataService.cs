using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Contracts;

public interface ISiteCategoryMenuDataService
{
    Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default);
}
