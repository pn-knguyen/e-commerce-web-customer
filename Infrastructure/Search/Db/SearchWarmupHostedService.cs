using e_commerce_web_customer.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace e_commerce_web_customer.Infrastructure.Search.Db;

public sealed class SearchWarmupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SearchWarmupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var productCatalog = scope.ServiceProvider.GetRequiredService<IProductCatalog>();
            await productCatalog.SearchAsync(
                new ProductCatalogSearchRequest(null, 8),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Search warm-up failed. The first live search request will rebuild the search cache.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
