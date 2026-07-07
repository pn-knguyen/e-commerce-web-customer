using e_commerce_web_customer.Application.Recommendations.Models;

namespace e_commerce_web_customer.Application.Recommendations.Abstractions;

public interface IRecommendationProvider
{
    int Priority { get; }

    bool CanHandle(ProductRecommendationRequest request);

    Task<IReadOnlyList<ProductRecommendationItem>> GetRecommendationsAsync(
        ProductRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
