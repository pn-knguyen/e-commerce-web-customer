using System.Globalization;
using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Data;
using Microsoft.EntityFrameworkCore;
using CatalogProductVariant = e_commerce_web_customer.Models.Entities.ProductVariant;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Services;

public sealed class CartAccessoryRecommendationService(
    EcommerceDbContext dbContext,
    IProductRecommendationService productRecommendationService) : ICartAccessoryRecommendationService
{
    public async Task<IReadOnlyList<CartAccessoryRecommendationGroup>> GetRecommendationsAsync(
        IReadOnlyList<CartSessionItem> cartItems,
        int limitPerProduct = 2,
        CancellationToken cancellationToken = default)
    {
        if (cartItems.Count == 0)
        {
            return [];
        }

        var cartVariants = await LoadCartVariantsAsync(
            cartItems,
            cancellationToken);
        if (cartVariants.Count == 0)
        {
            return [];
        }

        var cartVariantKeys = cartVariants
            .Select(GetVariantKey)
            .Concat(cartItems.Select(item => item.Id))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cartProductSlugs = cartVariants
            .Select(variant => variant.Product?.Slug)
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Select(slug => slug!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var variantByKey = cartVariants
            .SelectMany(variant => GetVariantLookupKeys(variant)
                .Select(key => new { Key = key, Variant = variant }))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Variant,
                StringComparer.OrdinalIgnoreCase);
        var processedParentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<CartAccessoryRecommendationGroup>();

        foreach (var cartItem in cartItems)
        {
            var cartItemKey = cartItem.Id?.Trim();
            if (string.IsNullOrWhiteSpace(cartItemKey)
                || !variantByKey.TryGetValue(cartItemKey, out var parentVariant)
                || parentVariant.Product is null)
            {
                continue;
            }

            var canonicalParentVariantKey = GetVariantKey(parentVariant);
            if (!processedParentKeys.Add(canonicalParentVariantKey))
            {
                continue;
            }

            var product = parentVariant.Product;
            var recommendations = await productRecommendationService.GetAccessoryUpsellsAsync(
                new ProductRecommendationRequest
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    BrandSlug = product.Brand?.Slug,
                    CategorySlug = product.Category?.Slug,
                    Surface = RecommendationSurface.ProductDetailAccessoryUpsell,
                    Limit = Math.Max(limitPerProduct * 4, 6)
                },
                cancellationToken);

            var groupItems = recommendations
                .Where(recommendation =>
                    !cartVariantKeys.Contains(recommendation.ProductVariantKey)
                    && (string.IsNullOrWhiteSpace(recommendation.ProductSlug)
                        || !cartProductSlugs.Contains(recommendation.ProductSlug)))
                .GroupBy(
                    recommendation => !string.IsNullOrWhiteSpace(recommendation.ProductVariantKey)
                        ? recommendation.ProductVariantKey
                        : recommendation.Url,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(item => item.Score)
                    .First())
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.CurrentPrice)
                .Take(NormalizeLimit(limitPerProduct))
                .ToList();

            if (groupItems.Count > 0)
            {
                groups.Add(new CartAccessoryRecommendationGroup
                {
                    ParentProductVariantKey = cartItemKey,
                    ParentProductName = cartItem.Name,
                    ParentVariantLabel = cartItem.Variant,
                    Items = groupItems
                });
            }
        }

        return groups;
    }

    private async Task<IReadOnlyList<CatalogProductVariant>> LoadCartVariantsAsync(
        IReadOnlyList<CartSessionItem> cartItems,
        CancellationToken cancellationToken)
    {
        var numericIds = new List<long>();
        var codes = new List<string>();
        foreach (var item in cartItems)
        {
            var key = item.Id?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                numericIds.Add(id);
            }
            else
            {
                codes.Add(key);
            }
        }

        if (numericIds.Count == 0 && codes.Count == 0)
        {
            return [];
        }

        return await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.IsActive
                && variant.Product != null
                && variant.Product.IsActive
                && ((numericIds.Count > 0 && numericIds.Contains(variant.Id))
                    || (codes.Count > 0 && codes.Contains(variant.Code))))
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Brand)
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Category)
            .ToListAsync(cancellationToken);
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return 2;
        }

        return Math.Min(limit, 8);
    }

    private static string GetVariantKey(CatalogProductVariant variant)
    {
        return !string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Code
            : variant.Id.ToString(CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string> GetVariantLookupKeys(CatalogProductVariant variant)
    {
        yield return variant.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(variant.Code))
        {
            yield return variant.Code;
        }
    }
}
