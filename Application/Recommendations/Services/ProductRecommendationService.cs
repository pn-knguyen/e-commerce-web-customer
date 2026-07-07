using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;

namespace e_commerce_web_customer.Application.Recommendations.Services;

public sealed class ProductRecommendationService(
    IEnumerable<IRecommendationProvider> providers) : IProductRecommendationService
{
    public async Task<IReadOnlyList<ProductRecommendationItem>> GetAccessoryUpsellsAsync(
        ProductRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = NormalizeLimit(request.Limit);
        var results = new List<ProductRecommendationItem>(limit);
        var seenVariantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers
            .Where(provider => provider.CanHandle(request))
            .OrderByDescending(provider => provider.Priority))
        {
            var recommendations = await provider.GetRecommendationsAsync(
                request,
                cancellationToken);

            foreach (var recommendation in recommendations)
            {
                if (!seenVariantKeys.Add(recommendation.ProductVariantKey))
                {
                    continue;
                }

                results.Add(recommendation);
                if (results.Count >= limit)
                {
                    return results;
                }
            }
        }

        return results;
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return 6;
        }

        return Math.Min(limit, 24);
    }
}
