using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Application.Services;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Services;

public sealed class NoOpCartAccessoryRecommendationService : ICartAccessoryRecommendationService
{
    public Task<IReadOnlyList<CartAccessoryRecommendationGroup>> GetRecommendationsAsync(
        IReadOnlyList<CartSessionItem> cartItems,
        int limitPerProduct = 2,
        CancellationToken cancellationToken = default)
    {
        _ = cartItems;
        _ = limitPerProduct;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<CartAccessoryRecommendationGroup>>([]);
    }
}
