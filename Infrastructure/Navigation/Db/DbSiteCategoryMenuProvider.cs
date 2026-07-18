using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Navigation.Db;

public sealed class DbSiteCategoryMenuProvider(
    ISiteCategoryMenuDataService siteCategoryMenuDataService,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate) : ISiteCategoryMenuProvider
{
    private const string CacheKey = "site-category-menu-v3";

    public Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateExclusiveAsync(
            CacheKey,
            () => dbQueryGate.RunAsync(
                () => siteCategoryMenuDataService.GetMenuAsync(CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
    }
}
