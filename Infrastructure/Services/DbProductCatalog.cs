using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Data;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbProductCatalog(
    EcommerceDbContext dbContext) : IProductCatalog
{
    public Task<IReadOnlyList<ProductReadModel>> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        return SearchCoreAsync(query, cancellationToken);
    }

    public async Task<ProductReadModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var normalizedId = id.Trim();
        var hasVariantId = long.TryParse(normalizedId, out var variantId);
        var product = await ProductsQuery()
            .FirstOrDefaultAsync(
                item => hasVariantId
                    ? item.ProductVariants.Any(variant => variant.Id == variantId)
                    : item.Slug == normalizedId ||
                      item.ProductVariants.Any(variant => variant.Code == normalizedId),
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var variant = product.ProductVariants
            .Where(item => item.IsActive)
            .OrderByDescending(item => hasVariantId && item.Id == variantId)
            .ThenByDescending(item => item.IsDefault)
            .ThenBy(item => item.Id)
            .FirstOrDefault();

        return variant is null ? null : DbProductMapper.ToReadModel(product, variant);
    }

    private async Task<IReadOnlyList<ProductReadModel>> SearchCoreAsync(
        string? query,
        CancellationToken cancellationToken)
    {
        var products = await ProductsQuery()
            .OrderByDescending(item => item.IsFeatured)
            .ThenByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var normalizedQuery = SearchTextNormalizer.Normalize(query ?? string.Empty);

        return products
            .Select(product =>
            {
                var variant = product.ProductVariants
                    .Where(item => item.IsActive)
                    .OrderByDescending(item => item.IsDefault)
                    .ThenBy(item => item.Id)
                    .First();

                return DbProductMapper.ToReadModel(product, variant);
            })
            .Where(product =>
                string.IsNullOrWhiteSpace(normalizedQuery) ||
                SearchTextNormalizer.Normalize(product.SearchText)
                    .Contains(normalizedQuery, StringComparison.Ordinal))
            .ToList();
    }

    private IQueryable<Models.Product> ProductsQuery()
    {
        return dbContext.Products
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.Category)
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages)
            .Where(item => item.IsActive && item.ProductVariants.Any(variant => variant.IsActive));
    }
}
