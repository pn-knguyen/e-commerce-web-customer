using System.Globalization;
using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Providers;

public sealed class CompatibilityFallbackRecommendationProvider(
    EcommerceDbContext dbContext,
    IEnumerable<ICompatibilityRule> compatibilityRules) : IRecommendationProvider
{
    private const string FallbackImageUrl = "/images/logo-techstore-icon.svg";

    public int Priority => 100;

    public bool CanHandle(ProductRecommendationRequest request)
    {
        return request.Surface == RecommendationSurface.ProductDetailAccessoryUpsell
            && compatibilityRules.Any(rule => rule.CanHandle(request));
    }

    public async Task<IReadOnlyList<ProductRecommendationItem>> GetRecommendationsAsync(
        ProductRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var activeRules = compatibilityRules
            .Where(rule => rule.CanHandle(request))
            .ToList();
        if (activeRules.Count == 0)
        {
            return [];
        }

        var brandSlugs = activeRules
            .SelectMany(rule => rule.CandidateBrandSlugs)
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var categorySlugs = activeRules
            .SelectMany(rule => rule.CandidateCategorySlugs)
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (brandSlugs.Length == 0 || categorySlugs.Length == 0)
        {
            return [];
        }

        var accessories = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Id != request.ProductId
                && product.IsActive
                && product.Brand != null
                && brandSlugs.Contains(product.Brand.Slug)
                && product.Category != null
                && categorySlugs.Contains(product.Category.Slug)
                && product.ProductVariants.Any(variant =>
                    variant.IsActive
                    && variant.Quantity > 0))
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .Include(product => product.ProductVariants)
                .ThenInclude(variant => variant.ProductVariantImages)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return accessories
            .SelectMany(accessory => activeRules
                .Select(rule => rule.Evaluate(accessory, request))
                .Where(candidate => candidate is not null)
                .Select(candidate => candidate!))
            .GroupBy(candidate => GetVariantKey(candidate.Variant), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Product.Name)
                .First())
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Product.Name)
            .Take(NormalizeLimit(request.Limit))
            .Select(ToRecommendationItem)
            .ToList();
    }

    private static ProductRecommendationItem ToRecommendationItem(
        RecommendationCandidate candidate)
    {
        var image = GetPrimaryImage(candidate.Variant);

        return new ProductRecommendationItem
        {
            ProductVariantKey = GetVariantKey(candidate.Variant),
            ProductSlug = candidate.Product.Slug,
            Url = BuildVariantDetailUrl(candidate.Product, candidate.Variant),
            Name = candidate.Product.Name,
            VariantLabel = BuildVariantLabel(candidate.Variant),
            ImageUrl = NormalizeImageUrl(image?.ImagePath),
            ImageAlt = BuildImageAlt(image, candidate.Product.Name, candidate.Variant),
            MemberOffer = candidate.MemberOffer,
            CurrentPrice = candidate.Variant.Price,
            OldPrice = null,
            Variants = BuildVariantItems(candidate.Product),
            Score = candidate.Score,
            Source = candidate.Source
        };
    }

    private static IReadOnlyList<ProductRecommendationVariantItem> BuildVariantItems(Product product)
    {
        return product.ProductVariants
            .Where(variant => variant.IsActive && variant.Quantity > 0)
            .OrderByDescending(variant => variant.IsDefault)
            .ThenBy(variant => variant.ColorName)
            .ThenBy(variant => variant.Id)
            .Select(variant =>
            {
                var image = GetPrimaryImage(variant);

                return new ProductRecommendationVariantItem
                {
                    ProductVariantKey = GetVariantKey(variant),
                    Url = BuildVariantDetailUrl(product, variant),
                    Label = BuildVariantLabel(variant),
                    ImageUrl = NormalizeImageUrl(image?.ImagePath),
                    ImageAlt = BuildImageAlt(image, product.Name, variant),
                    CurrentPrice = variant.Price,
                    Quantity = variant.Quantity,
                    IsDefault = variant.IsDefault
                };
            })
            .ToList();
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return 6;
        }

        return Math.Min(limit, 24);
    }

    private static string BuildVariantDetailUrl(Product product, ProductVariant variant)
    {
        return $"/product/{Uri.EscapeDataString(product.Slug)}?variant={Uri.EscapeDataString(GetVariantKey(variant))}";
    }

    private static string GetVariantKey(ProductVariant variant)
    {
        return !string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Code
            : variant.Id.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildVariantLabel(ProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.ColorName))
        {
            return variant.ColorName.Trim();
        }

        return !string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Code.Trim()
            : "Mặc định";
    }

    private static ProductVariantImage? GetPrimaryImage(ProductVariant variant)
    {
        return variant.ProductVariantImages
            .OrderBy(image => image.Position)
            .ThenBy(image => image.Id)
            .FirstOrDefault();
    }

    private static string BuildImageAlt(
        ProductVariantImage? image,
        string detailName,
        ProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(image?.AltText))
        {
            return image.AltText;
        }

        return string.IsNullOrWhiteSpace(variant.ColorName)
            ? detailName
            : $"{detailName} {variant.ColorName}";
    }

    private static string NormalizeImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return FallbackImageUrl;
        }

        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith('/'))
        {
            return imagePath;
        }

        return "/" + imagePath.TrimStart('/');
    }
}
