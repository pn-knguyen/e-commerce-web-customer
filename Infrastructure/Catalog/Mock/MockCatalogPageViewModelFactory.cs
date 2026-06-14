using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.Infrastructure.Home.Mock;
using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.ViewModels.Catalog;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Catalog.Mock;

public sealed class MockCatalogPageViewModelFactory : ICatalogPageViewModelFactory
{
    public Task<CatalogIndexViewModel> CreateAsync(ProductQuery query)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        IEnumerable<ProductCardViewModel> products = PhoneCategorySectionFactory.Create()
            .Tabs
            .SelectMany(tab => tab.Panel?.Products ?? []);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            products = products.Where(product =>
                product.Name.Contains(query.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (query.BrandId.HasValue)
        {
            var brandName = query.BrandId.Value switch
            {
                2 => "iPhone",
                3 => "Samsung",
                5 => "OPPO",
                6 => "Xiaomi",
                20 => "Sony",
                _ => null
            };

            products = brandName is null
                ? []
                : products.Where(product =>
                    product.Name.Contains(brandName, StringComparison.OrdinalIgnoreCase));
        }

        var filteredProducts = products.ToList();
        var totalItems = filteredProducts.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var pageProducts = filteredProducts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new CatalogIndexViewModel
        {
            Query = query,
            Products = pageProducts,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        });
    }
}
