using e_commerce_web_customer.ViewModels.Catalog;

namespace e_commerce_web_customer.Application.Catalog;

public interface ICategoryPageViewModelFactory
{
    Task<CategoryPageViewModel?> CreateAsync(
        CategoryPageRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CategoryPageRequest(
    string Slug,
    string? Brand = null,
    string? Sort = null);
