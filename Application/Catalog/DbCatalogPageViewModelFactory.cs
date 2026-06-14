using System.Globalization;
using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Catalog;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Catalog;

public sealed class DbCatalogPageViewModelFactory(IProductService productService)
    : ICatalogPageViewModelFactory
{
    public async Task<CatalogIndexViewModel> CreateAsync(ProductQuery query)
    {
        var products = await productService.GetAllAsync(query);

        return new CatalogIndexViewModel
        {
            Query = query,
            Products = products.Items.Select(MapToProductCard).ToList(),
            PageNumber = products.PageNumber,
            PageSize = products.PageSize,
            TotalItems = products.TotalItems,
            TotalPages = products.TotalPages,
            HasPreviousPage = products.HasPreviousPage,
            HasNextPage = products.HasNextPage
        };
    }

    private static ProductCardViewModel MapToProductCard(ProductDto product)
    {
        return new ProductCardViewModel
        {
            Id = product.VariantId.ToString(CultureInfo.InvariantCulture),
            Name = product.Name,
            Url = $"/product/{product.Slug}?variantId={product.VariantId}",
            ImageUrl = string.IsNullOrWhiteSpace(product.ProductImage)
                ? "/images/logo-techstore-icon.svg"
                : product.ProductImage,
            ImageAlt = product.Name,
            CurrentPrice = product.Price.HasValue
                ? $"{product.Price.Value.ToString("N0", CultureInfo.InvariantCulture)}đ"
                : "Liên hệ",
            PromotionNote = product.Description,
            DeliveryLabel = "Giao 2 giờ",
            RatingCount = product.RatingCount,
            ShowWishlistAction = false
        };
    }
}
