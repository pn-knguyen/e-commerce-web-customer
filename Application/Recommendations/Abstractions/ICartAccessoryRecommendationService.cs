using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Application.Services;

namespace e_commerce_web_customer.Application.Recommendations.Abstractions;

public interface ICartAccessoryRecommendationService
{
    Task<IReadOnlyList<CartAccessoryRecommendationGroup>> GetRecommendationsAsync(
        IReadOnlyList<CartSessionItem> cartItems,
        int limitPerProduct = 2,
        CancellationToken cancellationToken = default);
}
