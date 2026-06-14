using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.ViewModels.Catalog;

namespace e_commerce_web_customer.Application.Contracts;

public interface ICategoryPageDataService
{
    Task<CategoryPageViewModel?> CreateCategoryPageAsync(
        CategoryPageRequest request,
        CancellationToken cancellationToken = default);
}
