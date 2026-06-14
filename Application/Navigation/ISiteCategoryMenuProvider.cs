using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Navigation;

public interface ISiteCategoryMenuProvider
{
    Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default);
}
