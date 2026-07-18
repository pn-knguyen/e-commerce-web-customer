using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.ViewModels.Search;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Search.Db;

public sealed class DbSearchResultDataService(
    IProductCatalog productCatalog,
    IMemoryCache cache)
    : ISearchResultDataService
{
    private const int PageSize = 25;

    public Task<SearchResultPageViewModel> SearchAsync(
        SearchResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = SearchTextNormalizer.CleanQuery(request.Query);
        var sort = NormalizeSort(request.Sort);
        var category = request.Category?.Trim();
        var page = Math.Max(1, request.Page);
        var cacheKey = $"search-result-page-v3:{query}:{sort}:{category?.ToLowerInvariant() ?? "all"}:{page}";

        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => SearchUncachedAsync(
                new SearchResultRequest(query, sort, category, page),
                CancellationToken.None),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            });
    }

    private async Task<SearchResultPageViewModel> SearchUncachedAsync(
        SearchResultRequest request,
        CancellationToken cancellationToken)
    {
        var query = SearchTextNormalizer.CleanQuery(request.Query);
        var sort = NormalizeSort(request.Sort);
        var category = request.Category?.Trim();
        var allProducts = await productCatalog.SearchAsync(
            new ProductCatalogSearchRequest(
                query,
                Scope: ProductCatalogSearchScope.Products),
            cancellationToken);
        var products = string.IsNullOrWhiteSpace(category)
            ? allProducts
            : allProducts
                .Where(product => string.Equals(
                    product.CategorySlug,
                    category,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        products = sort switch
        {
            "price-desc" => products.OrderByDescending(item => item.CurrentPrice).ToList(),
            "price-asc" => products.OrderBy(item => item.CurrentPrice).ToList(),
            _ => products
        };
        var page = Math.Max(1, request.Page);
        var totalCount = products.Count;
        var pageProducts = products
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return new SearchResultPageViewModel
        {
            Query = query,
            TotalCount = totalCount,
            InitialProductCount = PageSize,
            CurrentPage = page,
            HasMoreProducts = page * PageSize < totalCount,
            Categories = new[]
                {
                    new SearchResultCategoryViewModel
                    {
                        Label = $"Tất cả ({allProducts.Count})",
                        Url = BuildSearchUrl(query, sort),
                        IsActive = string.IsNullOrWhiteSpace(category)
                    }
                }
                .Concat(allProducts
                    .Where(product => !string.IsNullOrWhiteSpace(product.CategoryName)
                        && !string.IsNullOrWhiteSpace(product.CategorySlug))
                    .GroupBy(
                        product => new
                        {
                            Slug = product.CategorySlug!,
                            Name = product.CategoryName!
                        })
                    .OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key.Name)
                    .Select(group => new SearchResultCategoryViewModel
                    {
                        Label = $"{group.Key.Name} ({group.Count()})",
                        Url = BuildSearchUrl(query, sort, group.Key.Slug),
                        IsActive = string.Equals(
                            category,
                            group.Key.Slug,
                            StringComparison.OrdinalIgnoreCase)
                    }))
                .ToList(),
            SortOptions =
            [
                CreateSortOption("Liên quan", "relevance", query, sort, category),
                CreateSortOption("Giá cao", "price-desc", query, sort, category),
                CreateSortOption("Giá thấp", "price-asc", query, sort, category)
            ],
            Products = pageProducts.Select(ProductViewModelMapper.ToProductCard).ToList()
        };
    }

    private static string BuildSearchUrl(
        string query,
        string? sort = null,
        string? category = null)
    {
        var parameters = new List<string>
        {
            $"q={Uri.EscapeDataString(query)}"
        };

        if (!string.IsNullOrWhiteSpace(sort)
            && !string.Equals(sort, "relevance", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            parameters.Add($"category={Uri.EscapeDataString(category)}");
        }

        return $"/search?{string.Join('&', parameters)}";
    }

    private static SearchResultSortOptionViewModel CreateSortOption(
        string label,
        string value,
        string query,
        string activeSort,
        string? category)
    {
        return new SearchResultSortOptionViewModel
        {
            Label = label,
            Value = value,
            Url = BuildSearchUrl(query, value, category),
            Icon = value switch
            {
                "price-desc" => "sort-desc",
                "price-asc" => "sort-asc",
                _ => "relevance"
            },
            IsActive = value == activeSort
        };
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "price-desc" => "price-desc",
            "price-asc" => "price-asc",
            _ => "relevance"
        };
    }
}
