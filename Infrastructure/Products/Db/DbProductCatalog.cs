using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Products.Db;

public sealed class DbProductCatalog(
    EcommerceDbContext dbContext,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate) : IProductCatalog
{
    private const int MaxSuggestionCandidates = 100;
    private const int MaxSearchTerms = 8;

    public Task<IReadOnlyList<ProductReadModel>> SearchAsync(
        ProductCatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = NormalizeQuery(request.Query);
        var effectiveLimit = request.Limit is > 0
            ? Math.Clamp(request.Limit.Value, 1, MaxSuggestionCandidates)
            : MaxSuggestionCandidates;
        var normalizedRequest = request with
        {
            Query = query,
            Limit = effectiveLimit
        };
        var cacheKey = $"product-search-v4:{normalizedRequest.Scope}:{effectiveLimit}:{query}";

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
        var terms = BuildSearchTerms(query);
        var candidateLimit = GetCandidateLimit(request.Limit);
        var searchIndex = await GetSearchIndexAsync();
        IEnumerable<ProductSearchIndexEntry> matchingEntries = searchIndex;

        foreach (var term in terms)
        {
            matchingEntries = matchingEntries.Where(entry =>
                entry.SearchText.Contains(term, StringComparison.Ordinal));
        }

        var candidateIds = matchingEntries
            .OrderByDescending(entry => entry.IsFeatured)
            .ThenByDescending(entry => entry.TotalSoldCount)
            .ThenByDescending(entry => entry.ViewsCount)
            .ThenBy(entry => entry.Id)
            .Take(candidateLimit ?? MaxSuggestionCandidates)
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
            .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Take(1));

        if (request.Scope == ProductCatalogSearchScope.Variants)
        {
            candidateDetails = candidateDetails
                .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.VariantAttributes)
                .ThenInclude(attribute => attribute.AttributeOption)
                    .ThenInclude(option => option!.Attribute);
        }

        var candidates = await candidateDetails
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var normalizedQuery = SearchTextNormalizer.Normalize(query);
        var searchResults = request.Scope == ProductCatalogSearchScope.Variants
            ? BuildVariantResults(candidates, normalizedQuery)
            : BuildProductResults(candidates, normalizedQuery);

        if (request.Limit is > 0)
        {
            searchResults = searchResults.Take(Math.Clamp(
                request.Limit.Value,
                1,
                MaxSuggestionCandidates));
        }

        return searchResults.ToList();
    }

    private Task<IReadOnlyList<ProductSearchIndexEntry>> GetSearchIndexAsync()
    {
        return cache.GetOrCreateExclusiveAsync(
            "product-search-index-v1",
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

        return rows
            .GroupBy(row => row.ProductId)
            .Select(group =>
            {
                var product = group.First();
                return new ProductSearchIndexEntry(
                product.ProductId,
                product.IsFeatured,
                product.TotalSoldCount,
                product.ViewsCount,
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
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Take(1))
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

    private static IEnumerable<ProductReadModel> BuildProductResults(
        IEnumerable<Product> candidates,
        string normalizedQuery)
    {
        return candidates
            .Select(product => new
            {
                Result = DbProductSearchMapper.Map(product),
                Relevance = DbProductSearchRanker.CalculateRelevance(
                    product,
                    normalizedQuery)
            })
            .OrderByDescending(item => string.IsNullOrWhiteSpace(normalizedQuery)
                ? item.Result.PopularityScore
                : item.Relevance)
            .ThenByDescending(item => item.Result.PopularityScore)
            .ThenBy(item => item.Result.Name)
            .Select(item => item.Result);
    }

    private static IEnumerable<ProductReadModel> BuildVariantResults(
        IEnumerable<Product> candidates,
        string normalizedQuery)
    {
        return candidates
            .SelectMany(product => product.ProductVariants
                .Where(variant => variant.IsActive)
                .Select(variant => new
                {
                    Result = DbProductSearchMapper.MapVariant(product, variant),
                    Relevance = DbProductSearchRanker.CalculateRelevance(
                        product,
                        variant,
                        normalizedQuery)
                }))
            .OrderByDescending(item => string.IsNullOrWhiteSpace(normalizedQuery)
                ? item.Result.PopularityScore
                : item.Relevance)
            .ThenByDescending(item => item.Result.PopularityScore)
            .ThenBy(item => item.Result.Name)
            .Select(item => item.Result);
    }

    private static string NormalizeQuery(string? query)
    {
        return SearchTextNormalizer.CleanQuery(query);
    }

    private static IReadOnlyList<string> BuildSearchTerms(string query)
    {
        return DbProductSearchRanker.BuildTerms(query, MaxSearchTerms);
    }

    private static int? GetCandidateLimit(int? requestedLimit)
    {
        if (requestedLimit is not > 0)
        {
            return null;
        }

        var resultLimit = Math.Clamp(
            requestedLimit.Value,
            1,
            MaxSuggestionCandidates);
        return Math.Min(
            MaxSuggestionCandidates,
            Math.Max(resultLimit * 4, 24));
    }

    private sealed record ProductSearchIndexRow(
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

    private sealed record ProductSearchIndexEntry(
        long Id,
        bool IsFeatured,
        int TotalSoldCount,
        int ViewsCount,
        string SearchText);
}
