using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Navigation.Db;

public sealed class DbSiteCategoryMenuProvider(
    ISiteCategoryMenuDataService siteCategoryMenuDataService,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate,
    EcommerceDbContext dbContext,
    ILogger<DbSiteCategoryMenuProvider> logger) : ISiteCategoryMenuProvider
{
    private const string CacheKey = "site-category-menu-v5";
    private const string LastGoodMenuCacheKey = "site-category-menu-last-good";

    public async Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categoryCacheToken = await dbQueryGate.RunAsync(
                () => CategoryCacheTokenBuilder.CreateAsync(dbContext, cancellationToken),
                cancellationToken);

            var menu = await cache.GetOrCreateExclusiveAsync(
                $"{CacheKey}:{categoryCacheToken}",
                () => dbQueryGate.RunAsync(
                    () => siteCategoryMenuDataService.GetMenuAsync(CancellationToken.None)),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                });

            cache.Set(
                LastGoodMenuCacheKey,
                menu,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });

            return menu;
        }
        catch (Exception exception) when (StorefrontDbExceptionDetector.IsTransient(exception))
        {
            if (cache.TryGetValue<SiteCategoryMenuViewModel>(
                    LastGoodMenuCacheKey,
                    out var cachedMenu)
                && cachedMenu is not null)
            {
                logger.LogWarning(
                    exception,
                    "Using the last cached site category menu after a transient database failure.");
                return cachedMenu;
            }

            logger.LogWarning(
                exception,
                "Returning an empty site category menu after a transient database failure before the menu cache was warmed.");
            return new SiteCategoryMenuViewModel
            {
                Items = []
            };
        }
    }
}
