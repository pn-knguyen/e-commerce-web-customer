using e_commerce_web_customer.Application.Recommendations.Models;

namespace e_commerce_web_customer.Application.Recommendations.Abstractions;

public interface IProductRecommendationService
{
    Task<IReadOnlyList<ProductRecommendationItem>> GetAccessoryUpsellsAsync(
        ProductRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
