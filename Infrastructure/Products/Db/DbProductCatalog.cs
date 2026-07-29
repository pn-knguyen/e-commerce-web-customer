using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Application.Search.ContentBased;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Models.Constants;
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
    private const string FallbackImageUrl = "/images/logo-techstore-icon.svg";
    private const string UnavailableLabel = "Tạm hết hàng";

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
        var cacheKey = $"product-search-cba-v3:{normalizedRequest.Scope}:{cacheLimit}:{query}";

        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => SearchUncachedAsync(normalizedRequest, CancellationToken.None),
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
        var snapshot = await GetSearchSnapshotAsync();
        var source = request.Scope == ProductCatalogSearchScope.Variants
            ? snapshot.Variants
            : snapshot.Products;
        var candidateLimit = GetCandidateLimit(request.Limit);
        var candidates = SelectCandidates(source, profile, candidateLimit);
        var searchResults = contentBasedSearchRanker
            .Rank(candidates.Select(candidate => candidate.Product), query, request.Limit)
            .Select(item => item.Product)
            .ToList();

        return searchResults;
    }

    private Task<ProductSearchSnapshot> GetSearchSnapshotAsync()
    {
        return cache.GetOrCreateExclusiveAsync(
            "product-search-snapshot-cba-v1",
            () => dbQueryGate.RunAsync(
                () => LoadSearchSnapshotAsync(CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
    }

    private async Task<ProductSearchSnapshot> LoadSearchSnapshotAsync(
        CancellationToken cancellationToken)
    {
        var variantRows = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive
                && variant.Product != null
                && variant.Product.IsActive
                && variant.Product.Brand != null
                && variant.Product.Brand.IsActive
                && variant.Product.Category != null
                && variant.Product.Category.IsActive)
            .Select(variant => new ProductSearchVariantRow(
                variant.Id,
                variant.Product!.Id,
                variant.Product.Name,
                variant.Product.Slug,
                variant.Product.Description,
                variant.Product.IsFeatured,
                variant.Product.TotalSoldCount,
                variant.Product.ViewsCount,
                variant.Product.RatingAverage,
                variant.Product.Brand!.Name,
                variant.Product.Brand.Slug,
                variant.Product.Category!.Name,
                variant.Product.Category.Slug,
                variant.Code,
                variant.Price,
                variant.SoldCount,
                variant.Quantity,
                variant.ColorName,
                variant.IsDefault,
                variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault(),
                variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .ThenBy(image => image.Id)
                    .Select(image => image.AltText)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        if (variantRows.Count == 0)
        {
            return new ProductSearchSnapshot([], []);
        }

        var productIds = variantRows
            .Select(row => row.ProductId)
            .Distinct()
            .ToArray();
        var variantIds = variantRows
            .Select(row => row.VariantId)
            .Distinct()
            .ToArray();
        var specificationRows = await dbContext.ProductSpecifications
            .AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => new ProductSearchSpecificationRow(
                item.ProductId,
                item.SpecificationId,
                item.Specification != null ? item.Specification.Name : string.Empty,
                item.Specification != null ? item.Specification.Key : string.Empty,
                item.Value,
                item.SortOrder))
            .ToListAsync(cancellationToken);
        var attributeRows = await dbContext.VariantAttributes
            .AsNoTracking()
            .Where(item => variantIds.Contains(item.ProductVariantId)
                && item.ProductVariant != null
                && item.ProductVariant.IsActive
                && item.AttributeOption != null
                && item.AttributeOption.Attribute != null)
            .Select(item => new ProductSearchAttributeRow(
                item.ProductVariantId,
                item.ProductVariant!.ProductId,
                item.AttributeOptionId,
                item.AttributeOption!.Attribute!.Code,
                item.AttributeOption.Attribute.Name,
                item.AttributeOption.Value,
                item.AttributeOption.Label))
            .ToListAsync(cancellationToken);
        var specificationsByProduct = specificationRows
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.SpecificationId)
                    .ToList());
        var attributesByProduct = attributeRows
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());
        var attributesByVariant = attributeRows
            .GroupBy(item => item.VariantId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var productEntries = new List<ProductSearchCacheEntry>();
        var variantEntries = new List<ProductSearchCacheEntry>();

        foreach (var productGroup in variantRows.GroupBy(row => row.ProductId))
        {
            var variants = productGroup.ToList();
            var product = variants[0];
            specificationsByProduct.TryGetValue(product.ProductId, out var specifications);
            attributesByProduct.TryGetValue(product.ProductId, out var productAttributes);
            var productModel = CreateProductReadModel(
                product,
                variants,
                specifications ?? [],
                productAttributes ?? []);
            productEntries.Add(CreateCacheEntry(productModel));

            foreach (var variant in variants)
            {
                attributesByVariant.TryGetValue(variant.VariantId, out var variantAttributes);
                var variantModel = CreateVariantReadModel(
                    product,
                    variant,
                    specifications ?? [],
                    variantAttributes ?? []);
                variantEntries.Add(CreateCacheEntry(variantModel));
            }
        }

        return new ProductSearchSnapshot(productEntries, variantEntries);
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

    private static IReadOnlyList<ProductSearchCacheEntry> SelectCandidates(
        IReadOnlyList<ProductSearchCacheEntry> source,
        ContentBasedSearchQuery profile,
        int candidateLimit)
    {
        IEnumerable<ProductSearchCacheEntry> matchingEntries = source;

        if (profile.CandidateTerms.Count > 0)
        {
            matchingEntries = matchingEntries.Where(entry =>
                profile.CandidateTerms.Any(term => EntryMatchesTerm(entry, term))
                || EntryMatchesCompactQuery(entry, profile.NormalizedQuery));
        }

        return matchingEntries
            .OrderByDescending(entry => CalculateIndexPriority(profile, entry))
            .ThenByDescending(entry => entry.Product.PopularityScore)
            .ThenBy(entry => entry.Product.CurrentPrice)
            .ThenBy(entry => entry.Product.Name)
            .Take(candidateLimit)
            .ToList();
    }

    private static ProductReadModel CreateProductReadModel(
        ProductSearchVariantRow product,
        IReadOnlyList<ProductSearchVariantRow> variants,
        IReadOnlyList<ProductSearchSpecificationRow> specifications,
        IReadOnlyList<ProductSearchAttributeRow> attributes)
    {
        var representativeVariant = variants
            .OrderByDescending(variant => variant.IsDefault)
            .ThenByDescending(variant => variant.Quantity > 0)
            .ThenByDescending(variant => !string.IsNullOrWhiteSpace(variant.ImagePath))
            .ThenByDescending(variant => variant.SoldCount)
            .ThenBy(variant => variant.Price)
            .ThenBy(variant => variant.VariantId)
            .First();
        var displayName = ProductDisplayNameNormalizer.ToBaseName(product.ProductName);
        var productSlug = GetProductSlug(product);
        var searchText = BuildProductSearchText(product, variants, specifications, attributes);

        return new ProductReadModel(
            product.ProductId.ToString(),
            displayName,
            $"/product/{productSlug}",
            NormalizeImageUrl(representativeVariant.ImagePath),
            string.IsNullOrWhiteSpace(representativeVariant.ImageAlt)
                ? displayName
                : representativeVariant.ImageAlt!,
            variants.Min(variant => variant.Price),
            null,
            0,
            null,
            null,
            searchText,
            new[] { product.ProductSlug }
                .Concat(variants.Select(variant => variant.VariantCode))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.CategorySlug,
            product.CategoryName,
            variants.Any(variant => variant.Quantity > 0) ? null : UnavailableLabel,
            product.RatingAverage > 0 ? product.RatingAverage : null,
            BuildSpecificationHighlights(specifications),
            CalculateProductPopularity(product, variants),
            product.BrandSlug,
            product.BrandName);
    }

    private static ProductReadModel CreateVariantReadModel(
        ProductSearchVariantRow product,
        ProductSearchVariantRow variant,
        IReadOnlyList<ProductSearchSpecificationRow> specifications,
        IReadOnlyList<ProductSearchAttributeRow> attributes)
    {
        var productSlug = GetProductSlug(product);
        var variantKey = string.IsNullOrWhiteSpace(variant.VariantCode)
            ? variant.VariantId.ToString()
            : variant.VariantCode;
        var displayName = BuildVariantName(product.ProductName, attributes);
        var searchText = BuildVariantSearchText(product, variant, specifications, attributes);

        return new ProductReadModel(
            variantKey,
            displayName,
            $"/product/{productSlug}?variant={Uri.EscapeDataString(variantKey)}",
            NormalizeImageUrl(variant.ImagePath),
            string.IsNullOrWhiteSpace(variant.ImageAlt) ? displayName : variant.ImageAlt!,
            variant.Price,
            null,
            0,
            null,
            null,
            searchText,
            new[] { product.ProductSlug, variant.VariantCode, variant.VariantCode.Replace("-", string.Empty) }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            product.CategorySlug,
            product.CategoryName,
            variant.Quantity > 0 ? null : UnavailableLabel,
            product.RatingAverage > 0 ? product.RatingAverage : null,
            BuildSpecificationHighlights(specifications, attributes),
            CalculateVariantPopularity(product, variant),
            product.BrandSlug,
            product.BrandName);
    }

    private static ProductSearchCacheEntry CreateCacheEntry(ProductReadModel product)
    {
        return new ProductSearchCacheEntry(
            product,
            SearchTextNormalizer.Normalize(product.SearchText),
            SearchTextNormalizer.Normalize($"{product.CategoryName} {product.CategorySlug}"));
    }

    private static string BuildProductSearchText(
        ProductSearchVariantRow product,
        IEnumerable<ProductSearchVariantRow> variants,
        IEnumerable<ProductSearchSpecificationRow> specifications,
        IEnumerable<ProductSearchAttributeRow> attributes)
    {
        return string.Join(
            ' ',
            new[]
            {
                product.ProductName,
                product.ProductSlug,
                product.Description,
                product.BrandName,
                product.BrandSlug,
                product.CategoryName,
                product.CategorySlug
            }
            .Concat(variants.SelectMany(variant => new[] { variant.VariantCode, variant.ColorName }))
            .Concat(specifications.Select(specification =>
                $"{specification.SpecificationName} {specification.SpecificationKey} {specification.Value}"))
            .Concat(attributes.Select(attribute =>
                $"{attribute.AttributeName} {attribute.AttributeCode} {attribute.Label} {attribute.Value}"))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildVariantSearchText(
        ProductSearchVariantRow product,
        ProductSearchVariantRow variant,
        IEnumerable<ProductSearchSpecificationRow> specifications,
        IReadOnlyList<ProductSearchAttributeRow> attributes)
    {
        return string.Join(
            ' ',
            new[]
            {
                BuildProductSearchText(product, [variant], specifications, attributes),
                BuildVariantName(product.ProductName, attributes),
                variant.VariantCode,
                variant.VariantCode.Replace("-", string.Empty),
                variant.ColorName
            }
            .Concat(attributes.Select(attribute =>
                $"{attribute.AttributeName} {attribute.Label} {attribute.Value}"))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildVariantName(
        string productName,
        IReadOnlyList<ProductSearchAttributeRow> attributes)
    {
        var baseName = productName.Trim();
        var normalizedBaseName = NormalizeDisplayToken(baseName);
        var variantParts = attributes
            .Where(attribute => !string.Equals(
                attribute.AttributeCode,
                CatalogAttributeCodes.Color,
                StringComparison.OrdinalIgnoreCase))
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Label))
            .OrderBy(attribute => GetAttributeDisplayOrder(
                attribute.AttributeCode,
                attribute.AttributeName,
                attribute.Value,
                attribute.Label))
            .ThenBy(attribute => attribute.AttributeOptionId)
            .Select(attribute => attribute.Label.Trim())
            .Where(label => !normalizedBaseName.Contains(
                NormalizeDisplayToken(label),
                StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return variantParts.Count == 0
            ? baseName
            : $"{baseName} {string.Join(" ", variantParts)}";
    }

    private static IReadOnlyList<string> BuildSpecificationHighlights(
        IEnumerable<ProductSearchSpecificationRow> specifications,
        IEnumerable<ProductSearchAttributeRow>? attributes = null)
    {
        var specificationHighlights = specifications
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.SpecificationId)
            .Select(item => string.IsNullOrWhiteSpace(item.SpecificationName)
                ? item.Value
                : $"{item.SpecificationName}: {item.Value}");
        var attributeHighlights = attributes?
            .Select(item => string.IsNullOrWhiteSpace(item.Label) ? item.Value : item.Label)
            ?? [];

        return specificationHighlights
            .Concat(attributeHighlights)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }

    private static int CalculateProductPopularity(
        ProductSearchVariantRow product,
        IReadOnlyList<ProductSearchVariantRow> variants)
    {
        return (product.IsFeatured ? 1_000_000 : 0)
            + Math.Min(variants.Sum(variant => variant.SoldCount), 50_000) * 20
            + Math.Min(product.TotalSoldCount, 50_000) * 10
            + Math.Min(product.ViewsCount, 100_000)
            + (variants.Any(variant => variant.Quantity > 0) ? 5_000 : 0);
    }

    private static int CalculateVariantPopularity(
        ProductSearchVariantRow product,
        ProductSearchVariantRow variant)
    {
        return (product.IsFeatured ? 1_000_000 : 0)
            + Math.Min(variant.SoldCount, 50_000) * 25
            + Math.Min(product.TotalSoldCount, 50_000) * 10
            + Math.Min(product.ViewsCount, 100_000)
            + (variant.Quantity > 0 ? 5_000 : 0)
            + (variant.IsDefault ? 1_000 : 0);
    }

    private static int CalculateIndexPriority(
        ContentBasedSearchQuery profile,
        ProductSearchCacheEntry entry)
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

    private static bool EntryMatchesTerm(ProductSearchCacheEntry entry, string term)
    {
        return ContainsToken(entry.SearchText, term)
            || entry.SearchText.Contains(term, StringComparison.Ordinal);
    }

    private static bool EntryMatchesCompactQuery(
        ProductSearchCacheEntry entry,
        string normalizedQuery)
    {
        var compactQuery = Compact(normalizedQuery);
        return compactQuery.Length >= 3
            && Compact(entry.SearchText).Contains(compactQuery, StringComparison.Ordinal);
    }

    private static bool EntryCategoryMatchesTerm(ProductSearchCacheEntry entry, string term)
    {
        var normalizedTerm = SearchTextNormalizer.Normalize(term);
        return normalizedTerm.Length > 0
            && (entry.CategoryText.Contains(normalizedTerm, StringComparison.Ordinal)
                || Compact(entry.CategoryText).Contains(Compact(normalizedTerm), StringComparison.Ordinal));
    }

    private static int GetCandidateLimit(int? requestedLimit)
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

    private static int GetAttributeDisplayOrder(
        string? code,
        string? name,
        string? value,
        string? label)
    {
        var normalizedCode = code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedCode is CatalogAttributeCodes.Ram or CatalogAttributeCodes.RamCapacity)
        {
            return 0;
        }

        if (normalizedCode is CatalogAttributeCodes.Rom
            or CatalogAttributeCodes.Storage
            or CatalogAttributeCodes.InternalStorage)
        {
            return 1;
        }

        var searchableText = string.Join(' ', code, name, value, label).ToLowerInvariant();

        if (ContainsAny(searchableText, CatalogAttributeCodes.Ram, "bo nho ram", "bộ nhớ ram"))
        {
            return 0;
        }

        if (ContainsAny(
                searchableText,
                CatalogAttributeCodes.Rom,
                CatalogAttributeCodes.Storage,
                CatalogAttributeCodes.InternalStorage,
                "internal-storage",
                "capacity",
                "dung luong",
                "dung lượng",
                "luu tru",
                "lưu trữ",
                "bo nho trong",
                "bộ nhớ trong"))
        {
            return 1;
        }

        return 100;
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsToken(string text, string token)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(item => item == token);
    }

    private static string NormalizeQuery(string? query)
    {
        return SearchTextNormalizer.CleanQuery(query);
    }

    private static string GetProductSlug(ProductSearchVariantRow product)
    {
        return string.IsNullOrWhiteSpace(product.ProductSlug)
            ? product.ProductId.ToString()
            : product.ProductSlug;
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

    private static string NormalizeDisplayToken(string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string Compact(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private sealed record ProductSearchSnapshot(
        IReadOnlyList<ProductSearchCacheEntry> Products,
        IReadOnlyList<ProductSearchCacheEntry> Variants);

    private sealed record ProductSearchCacheEntry(
        ProductReadModel Product,
        string SearchText,
        string CategoryText);

    private sealed record ProductSearchVariantRow(
        long VariantId,
        long ProductId,
        string ProductName,
        string ProductSlug,
        string? Description,
        bool IsFeatured,
        int TotalSoldCount,
        int ViewsCount,
        decimal RatingAverage,
        string BrandName,
        string BrandSlug,
        string CategoryName,
        string CategorySlug,
        string VariantCode,
        decimal Price,
        int SoldCount,
        int Quantity,
        string? ColorName,
        bool IsDefault,
        string? ImagePath,
        string? ImageAlt);

    private sealed record ProductSearchSpecificationRow(
        long ProductId,
        long SpecificationId,
        string SpecificationName,
        string SpecificationKey,
        string Value,
        int SortOrder);

    private sealed record ProductSearchAttributeRow(
        long VariantId,
        long ProductId,
        long AttributeOptionId,
        string AttributeCode,
        string AttributeName,
        string Value,
        string Label);
}
