using CatalogProduct = e_commerce_web_customer.Models.Entities.Product;
using CatalogProductVariant = e_commerce_web_customer.Models.Entities.ProductVariant;

namespace e_commerce_web_customer.Application.Recommendations.Models;

public sealed record RecommendationCandidate(
    CatalogProduct Product,
    CatalogProductVariant Variant,
    string MemberOffer,
    int Score,
    string Source);
