using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Application.Search.ContentBased;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Products.Db;

public sealed class DbProductCatalog(
    EcommerceDbContext dbContext,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate,
    IContentBasedSearchRanker contentBasedSearchRanker) : IProductCatalog
{
    private const int MaxSuggestionResults = 100;
    private const int MaxSearchCandidates = 500;

    public Task<IReadOnlyList<ProductReadModel>> SearchAsync(
        ProductCatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = NormalizeQuery(request.Query);
        var effectiveLimit = request.Limit is > 0
            ? Math.Clamp(request.Limit.Value, 1, MaxSuggestionResults)
            : (int?)null;
        var normalizedRequest = request with
        {
            Query = query,
            Limit = effectiveLimit
        };
        var cacheLimit = effectiveLimit?.ToString() ?? "all";
        var cacheKey = $"product-search-cba-v2:{normalizedRequest.Scope}:{cacheLimit}:{query}";

        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => dbQueryGate.RunAsync(
                () => SearchUncachedAsync(normalizedRequest, CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
    }

    private async Task<IReadOnlyList<ProductReadModel>> SearchUncachedAsync(
        ProductCatalogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = NormalizeQuery(request.Query);
        var profile = contentBasedSearchRanker.CreateQuery(query);
        var terms = profile.CandidateTerms;
        var candidateLimit = GetCandidateLimit(request.Limit);
        var searchIndex = await GetSearchIndexAsync();
        IEnumerable<ProductSearchIndexEntry> matchingEntries = searchIndex;

        if (terms.Count > 0)
        {
            matchingEntries = matchingEntries.Where(entry =>
                terms.Any(term => EntryMatchesTerm(entry, term))
                || EntryMatchesCompactQuery(entry, profile.NormalizedQuery));
        }

        var candidateIds = matchingEntries
            .OrderByDescending(entry => CalculateIndexPriority(profile, entry))
            .ThenByDescending(entry => entry.IsFeatured)
            .ThenByDescending(entry => entry.TotalSoldCount)
            .ThenByDescending(entry => entry.ViewsCount)
            .ThenBy(entry => entry.Id)
            .Take(candidateLimit ?? MaxSearchCandidates)
            .Select(entry => entry.Id)
            .ToList();

        if (candidateIds.Count == 0)
        {
            return [];
        }

        IQueryable<Product> candidateDetails = BuildActiveProductQuery()
            .Where(product => candidateIds.Contains(product.Id))
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .Include(product => product.ProductSpecifications)
                .ThenInclude(specification => specification.Specification)
            .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Take(1))
            .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.VariantAttributes)
                .ThenInclude(attribute => attribute.AttributeOption)
                    .ThenInclude(option => option!.Attribute);

        var candidates = await candidateDetails
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var searchResults = request.Scope == ProductCatalogSearchScope.Variants
            ? BuildVariantResults(candidates, query)
            : BuildProductResults(candidates, query);

        if (request.Limit is > 0)
        {
            searchResults = searchResults.Take(Math.Clamp(
                request.Limit.Value,
                1,
                MaxSuggestionResults));
        }

        return searchResults.ToList();
    }

    private Task<IReadOnlyList<ProductSearchIndexEntry>> GetSearchIndexAsync()
    {
        return cache.GetOrCreateExclusiveAsync(
            "product-search-index-cba-v2",
            () => LoadSearchIndexAsync(CancellationToken.None),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
    }

    private async Task<IReadOnlyList<ProductSearchIndexEntry>> LoadSearchIndexAsync(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive
                && variant.Product != null
                && variant.Product.IsActive
                && variant.Product.Brand != null
                && variant.Product.Brand.IsActive
                && variant.Product.Category != null
                && variant.Product.Category.IsActive)
            .Select(variant => new ProductSearchIndexRow(
                variant.Id,
                variant.Product!.Id,
                variant.Product.Name,
                variant.Product.Slug,
                variant.Product.Brand!.Name,
                variant.Product.Brand.Slug,
                variant.Product.Category!.Name,
                variant.Product.Category.Slug,
                variant.Product.IsFeatured,
                variant.Product.TotalSoldCount,
                variant.Product.ViewsCount,
                variant.Code,
                variant.ColorName))
            .ToListAsync(cancellationToken);

        var productIds = rows
            .Select(row => row.ProductId)
            .Distinct()
            .ToArray();
        var specificationTexts = await dbContext.ProductSpecifications
            .AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => new ProductSearchIndexTextRow(
                item.ProductId,
                ((item.Specification != null ? item.Specification.Name : string.Empty) + " "
                    + (item.Specification != null ? item.Specification.Key : string.Empty) + " "
                    + item.Value).Trim()))
            .ToListAsync(cancellationToken);
        var attributeTexts = await dbContext.VariantAttributes
            .AsNoTracking()
            .Where(item => item.ProductVariant != null
                && item.ProductVariant.IsActive
                && productIds.Contains(item.ProductVariant.ProductId)
                && item.AttributeOption != null
                && item.AttributeOption.Attribute != null)
            .Select(item => new ProductSearchIndexTextRow(
                item.ProductVariant!.ProductId,
                (item.AttributeOption!.Attribute!.Name + " "
                    + item.AttributeOption.Attribute.Code + " "
                    + item.AttributeOption.Label + " "
                    + item.AttributeOption.Value).Trim()))
            .ToListAsync(cancellationToken);
        var specificationsByProduct = specificationTexts
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Text).ToList());
        var attributesByProduct = attributeTexts
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Text).ToList());

        return rows
            .GroupBy(row => row.ProductId)
            .Select(group =>
            {
                var product = group.First();
                specificationsByProduct.TryGetValue(product.ProductId, out var specificationText);
                attributesByProduct.TryGetValue(product.ProductId, out var attributeText);
                return new ProductSearchIndexEntry(
                product.ProductId,
                product.IsFeatured,
                product.TotalSoldCount,
                product.ViewsCount,
                SearchTextNormalizer.Normalize($"{product.CategoryName} {product.CategorySlug}"),
                SearchTextNormalizer.Normalize(string.Join(
                    ' ',
                    new[]
                    {
                        product.Name,
                        product.Slug,
                        product.BrandName,
                        product.BrandSlug,
                        product.CategoryName,
                        product.CategorySlug
                    }
                    .Concat(group.SelectMany(row => new[] { row.Code, row.ColorName }))
                    .Concat(specificationText ?? [])
                    .Concat(attributeText ?? [])
                    .Where(value => !string.IsNullOrWhiteSpace(value)))));
            })
            .ToList();
    }

    public Task<ProductReadModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var normalizedId = id.Trim();
        if (string.IsNullOrWhiteSpace(normalizedId))
        {
            return Task.FromResult<ProductReadModel?>(null);
        }

        var cacheKey = $"product-read-model-v2:{normalizedId.ToLowerInvariant()}";
        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => dbQueryGate.RunAsync(
                () => GetByIdUncachedAsync(normalizedId, CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
    }

    private async Task<ProductReadModel?> GetByIdUncachedAsync(
        string normalizedId,
        CancellationToken cancellationToken)
    {

        var products = BuildActiveProductQuery();
        if (long.TryParse(normalizedId, out var numericId))
        {
            products = products.Where(product =>
                product.Id == numericId
                || product.ProductVariants.Any(variant => variant.Id == numericId));
        }
        else
        {
            products = products.Where(product =>
                product.Slug == normalizedId
                || product.ProductVariants.Any(variant => variant.Code == normalizedId));
        }

        var product = await products
            .Include(item => item.Brand)
            .Include(item => item.Category)
            .Include(item => item.ProductSpecifications)
                .ThenInclude(specification => specification.Specification)
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Take(1))
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.VariantAttributes)
                .ThenInclude(attribute => attribute.AttributeOption)
                    .ThenInclude(option => option!.Attribute)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? null : DbProductSearchMapper.Map(product);
    }

    private IQueryable<Product> BuildActiveProductQuery()
    {
        return dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive
                && product.Brand != null
                && product.Brand.IsActive
                && product.Category != null
                && product.Category.IsActive
                && product.ProductVariants.Any(variant => variant.IsActive));
    }

    private IEnumerable<ProductReadModel> BuildProductResults(
        IEnumerable<Product> candidates,
        string query)
    {
        var products = candidates.Select(DbProductSearchMapper.Map);
        return contentBasedSearchRanker
            .Rank(products, query)
            .Select(item => item.Product);
    }

    private IEnumerable<ProductReadModel> BuildVariantResults(
        IEnumerable<Product> candidates,
        string query)
    {
        var variants = candidates
            .SelectMany(product => product.ProductVariants
                .Where(variant => variant.IsActive)
                .Select(variant => DbProductSearchMapper.MapVariant(product, variant)));
        return contentBasedSearchRanker
            .Rank(variants, query)
            .Select(item => item.Product);
    }

    private static string NormalizeQuery(string? query)
    {
        return SearchTextNormalizer.CleanQuery(query);
    }

    private static int? GetCandidateLimit(int? requestedLimit)
    {
        if (requestedLimit is not > 0)
        {
            return MaxSearchCandidates;
        }

        var resultLimit = Math.Clamp(
            requestedLimit.Value,
            1,
            MaxSuggestionResults);
        return Math.Min(
            MaxSearchCandidates,
            Math.Max(resultLimit * 8, 40));
    }

    private static int CalculateIndexPriority(
        ContentBasedSearchQuery profile,
        ProductSearchIndexEntry entry)
    {
        if (!profile.HasQuery)
        {
            return 0;
        }

        var compactQuery = Compact(profile.NormalizedQuery);
        var score = 0;
        if (profile.PreferredCategoryTerms.Count > 0
            && profile.PreferredCategoryTerms.Any(term => EntryCategoryMatchesTerm(entry, term)))
        {
            score += 650;
        }

        if (compactQuery.Length >= 3
            && Compact(entry.SearchText).Contains(compactQuery, StringComparison.Ordinal))
        {
            score += 300;
        }

        foreach (var term in profile.CandidateTerms)
        {
            if (ContainsToken(entry.SearchText, term))
            {
                score += 40;
            }
            else if (entry.SearchText.Contains(term, StringComparison.Ordinal))
            {
                score += 20;
            }
        }

        return score;
    }

    private static bool EntryMatchesTerm(ProductSearchIndexEntry entry, string term)
    {
        return ContainsToken(entry.SearchText, term)
            || entry.SearchText.Contains(term, StringComparison.Ordinal);
    }

    private static bool EntryMatchesCompactQuery(
        ProductSearchIndexEntry entry,
        string normalizedQuery)
    {
        var compactQuery = Compact(normalizedQuery);
        return compactQuery.Length >= 3
            && Compact(entry.SearchText).Contains(compactQuery, StringComparison.Ordinal);
    }

    private static bool EntryCategoryMatchesTerm(ProductSearchIndexEntry entry, string term)
    {
        var normalizedTerm = SearchTextNormalizer.Normalize(term);
        return normalizedTerm.Length > 0
            && (entry.CategoryText.Contains(normalizedTerm, StringComparison.Ordinal)
                || Compact(entry.CategoryText).Contains(Compact(normalizedTerm), StringComparison.Ordinal));
    }

    private static bool ContainsToken(string text, string token)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(item => item == token);
    }

    private static string Compact(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private sealed record ProductSearchIndexRow(
        long VariantId,
        long ProductId,
        string Name,
        string Slug,
        string BrandName,
        string BrandSlug,
        string CategoryName,
        string CategorySlug,
        bool IsFeatured,
        int TotalSoldCount,
        int ViewsCount,
        string Code,
        string? ColorName);

    private sealed record ProductSearchIndexTextRow(
        long ProductId,
        string Text);

    private sealed record ProductSearchIndexEntry(
        long Id,
        bool IsFeatured,
        int TotalSoldCount,
        int ViewsCount,
        string CategoryText,
        string SearchText);
}
