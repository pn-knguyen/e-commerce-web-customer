using System.Globalization;
using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Algorithms;
using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using CatalogProduct = e_commerce_web_customer.Models.Entities.Product;
using CatalogProductVariant = e_commerce_web_customer.Models.Entities.ProductVariant;
using CatalogProductVariantImage = e_commerce_web_customer.Models.Entities.ProductVariantImage;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Providers;

public sealed class AprioriOrderRecommendationProvider(
    EcommerceDbContext dbContext,
    IEnumerable<ICompatibilityRule> compatibilityRules) : IRecommendationProvider
{
    private const string FallbackImageUrl = "/images/logo-techstore-icon.svg";
    private const string SourceName = "apriori-order";
    private const int MaxTrainingOrders = 500;
    private const int MaxSourceTrainingOrders = 1000;

    public int Priority => 200;

    public bool CanHandle(ProductRecommendationRequest request)
    {
        return request.Surface == RecommendationSurface.ProductDetailAccessoryUpsell
            && !string.IsNullOrWhiteSpace(request.ProductSlug);
    }

    public async Task<IReadOnlyList<ProductRecommendationItem>> GetRecommendationsAsync(
        ProductRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = NormalizeLimit(request.Limit);
        var ruleLimit = limit * 4;
        var transactions = await LoadOrderTransactionsAsync(cancellationToken);
        if (transactions.Count == 0)
        {
            return [];
        }

        var algorithm = new AprioriAlgorithm(minSupport: 0.03, minConfidence: 0.15);
        var rules = algorithm
            .GenerateRules(transactions)
            .Where(rule => rule.SourceSlug.Equals(request.ProductSlug, StringComparison.OrdinalIgnoreCase))
            .Take(ruleLimit)
            .ToList();

        AddSourceScopedRules(
            rules,
            transactions,
            request.ProductSlug,
            ruleLimit);

        if (rules.Count < ruleLimit)
        {
            var sourceTransactions = await LoadSourceOrderTransactionsAsync(
                request.ProductSlug,
                cancellationToken);
            AddSourceScopedRules(
                rules,
                sourceTransactions,
                request.ProductSlug,
                ruleLimit);
        }

        if (rules.Count == 0)
        {
            return [];
        }

        var targetSlugs = rules
            .Select(rule => rule.TargetSlug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive
                && targetSlugs.Contains(product.Slug)
                && product.ProductVariants.Any(variant =>
                    variant.IsActive
                    && variant.Quantity > 0))
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .Include(product => product.ProductVariants)
                .ThenInclude(variant => variant.ProductVariantImages)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var productBySlug = products.ToDictionary(
            product => product.Slug,
            StringComparer.OrdinalIgnoreCase);
        var activeCompatibilityRules = compatibilityRules
            .Where(rule => rule.CanHandle(request))
            .ToList();

        var recommendations = new List<ProductRecommendationItem>();
        var seenVariantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            if (!productBySlug.TryGetValue(rule.TargetSlug, out var product))
            {
                continue;
            }

            var candidate = BuildCompatibleCandidate(
                product,
                request,
                activeCompatibilityRules);
            if (candidate is null)
            {
                continue;
            }

            var item = ToRecommendationItem(
                candidate,
                rule.Confidence,
                rule.Support);
            if (!seenVariantKeys.Add(item.ProductVariantKey))
            {
                continue;
            }

            recommendations.Add(item);
            if (recommendations.Count >= limit)
            {
                break;
            }
        }

        return recommendations;
    }

    private async Task<IReadOnlyList<HashSet<string>>> LoadOrderTransactionsAsync(
        CancellationToken cancellationToken)
    {
        var recentOrderIds = await dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.OrderStatus != OrderStatus.Cancelled
                && order.OrderStatus != OrderStatus.Returned)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => order.Id)
            .Take(MaxTrainingOrders)
            .ToListAsync(cancellationToken);

        return await LoadTransactionsByOrderIdsAsync(
            recentOrderIds,
            cancellationToken);
    }

    private async Task<IReadOnlyList<HashSet<string>>> LoadSourceOrderTransactionsAsync(
        string productSlug,
        CancellationToken cancellationToken)
    {
        var sourceOrderRows = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Order != null
                && item.Order.OrderStatus != OrderStatus.Cancelled
                && item.Order.OrderStatus != OrderStatus.Returned
                && item.ProductVariant != null
                && item.ProductVariant.Product != null
                && item.ProductVariant.Product.Slug == productSlug)
            .Select(item => new
            {
                item.OrderId,
                item.Order!.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var sourceOrderIds = sourceOrderRows
            .GroupBy(row => row.OrderId)
            .OrderByDescending(group => group.Max(row => row.CreatedAt))
            .Take(MaxSourceTrainingOrders)
            .Select(group => group.Key)
            .ToList();

        return await LoadTransactionsByOrderIdsAsync(
            sourceOrderIds,
            cancellationToken);
    }

    private async Task<IReadOnlyList<HashSet<string>>> LoadTransactionsByOrderIdsAsync(
        IReadOnlyCollection<long> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return [];
        }

        var orderIdSet = orderIds.ToHashSet();

        var rows = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item =>
                orderIdSet.Contains(item.OrderId)
                && item.ProductVariant != null
                && item.ProductVariant.Product != null
                && item.ProductVariant.Product.IsActive)
            .Select(item => new
            {
                item.OrderId,
                ProductSlug = item.ProductVariant!.Product!.Slug
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => !string.IsNullOrWhiteSpace(row.ProductSlug))
            .GroupBy(row => row.OrderId)
            .Select(group => group
                .Select(row => row.ProductSlug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase))
            .Where(transaction => transaction.Count >= 2)
            .ToList();
    }

    private static void AddSourceScopedRules(
        List<ProductAssociationRule> rules,
        IReadOnlyList<HashSet<string>> transactions,
        string sourceSlug,
        int limit)
    {
        if (rules.Count >= limit || transactions.Count == 0)
        {
            return;
        }

        var existingTargets = rules
            .Select(rule => rule.TargetSlug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in BuildSourceScopedRules(transactions, sourceSlug))
        {
            if (!existingTargets.Add(rule.TargetSlug))
            {
                continue;
            }

            rules.Add(rule);
            if (rules.Count >= limit)
            {
                break;
            }
        }
    }

    private static IReadOnlyList<ProductAssociationRule> BuildSourceScopedRules(
        IReadOnlyList<HashSet<string>> transactions,
        string sourceSlug)
    {
        var sourceTransactions = transactions
            .Where(transaction => transaction.Contains(sourceSlug))
            .ToList();
        if (sourceTransactions.Count == 0)
        {
            return [];
        }

        var targetCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var transaction in sourceTransactions)
        {
            foreach (var targetSlug in transaction)
            {
                if (targetSlug.Equals(sourceSlug, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                targetCounts[targetSlug] = targetCounts.GetValueOrDefault(targetSlug) + 1;
            }
        }

        return targetCounts
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new ProductAssociationRule
            {
                SourceSlug = sourceSlug,
                TargetSlug = item.Key,
                Confidence = (double)item.Value / sourceTransactions.Count,
                Support = (double)item.Value / transactions.Count
            })
            .ToList();
    }

    private static RecommendationCandidate? BuildCompatibleCandidate(
        CatalogProduct product,
        ProductRecommendationRequest request,
        IReadOnlyList<ICompatibilityRule> activeCompatibilityRules)
    {
        var matchingRules = activeCompatibilityRules
            .Where(rule => IsCandidateScope(rule, product))
            .ToList();

        if (matchingRules.Count == 0)
        {
            return null;
        }

        return matchingRules
            .Select(rule => rule.Evaluate(product, request))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();
    }

    private static bool IsCandidateScope(
        ICompatibilityRule rule,
        CatalogProduct product)
    {
        return product.Brand is not null
            && product.Category is not null
            && rule.CandidateBrandSlugs.Contains(product.Brand.Slug, StringComparer.OrdinalIgnoreCase)
            && rule.CandidateCategorySlugs.Contains(product.Category.Slug, StringComparer.OrdinalIgnoreCase);
    }

    private static ProductRecommendationItem ToRecommendationItem(
        RecommendationCandidate candidate,
        double confidence,
        double support)
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
            Score = 2_000 + candidate.Score + (int)Math.Round(confidence * 1_000) + (int)Math.Round(support * 100),
            Source = SourceName
        };
    }

    private static IReadOnlyList<ProductRecommendationVariantItem> BuildVariantItems(
        CatalogProduct product)
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

    private static CatalogProductVariant? GetDefaultActiveVariant(CatalogProduct product)
    {
        return product.ProductVariants
            .Where(variant => variant.IsActive && variant.Quantity > 0)
            .OrderByDescending(variant => variant.IsDefault)
            .ThenBy(variant => variant.Id)
            .FirstOrDefault();
    }

    private static string BuildVariantDetailUrl(CatalogProduct product, CatalogProductVariant variant)
    {
        return $"/product/{Uri.EscapeDataString(product.Slug)}?variant={Uri.EscapeDataString(GetVariantKey(variant))}";
    }

    private static string GetVariantKey(CatalogProductVariant variant)
    {
        return !string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Code
            : variant.Id.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildVariantLabel(CatalogProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.ColorName))
        {
            return variant.ColorName.Trim();
        }

        return !string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Code.Trim()
            : "Mặc định";
    }

    private static CatalogProductVariantImage? GetPrimaryImage(CatalogProductVariant variant)
    {
        return variant.ProductVariantImages
            .OrderBy(image => image.Position)
            .ThenBy(image => image.Id)
            .FirstOrDefault();
    }

    private static string BuildImageAlt(
        CatalogProductVariantImage? image,
        string detailName,
        CatalogProductVariant variant)
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
