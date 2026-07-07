using e_commerce_web_customer.Application.Recommendations.Models;
using CatalogProduct = e_commerce_web_customer.Models.Entities.Product;

namespace e_commerce_web_customer.Application.Recommendations.Abstractions;

public interface ICompatibilityRule
{
    string Name { get; }
    IReadOnlyCollection<string> CandidateBrandSlugs { get; }
    IReadOnlyCollection<string> CandidateCategorySlugs { get; }

    bool CanHandle(ProductRecommendationRequest request);

    RecommendationCandidate? Evaluate(
        CatalogProduct accessory,
        ProductRecommendationRequest request);
}
