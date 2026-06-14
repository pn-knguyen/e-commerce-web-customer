using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Models;

namespace e_commerce_web_customer.Infrastructure.Services;

internal static class DbProductMapper
{
    private const string FallbackImage = "/images/logo-techstore-icon.svg";

    public static ProductReadModel ToReadModel(Product product, ProductVariant variant)
    {
        var image = variant.ProductVariantImages
            .OrderBy(item => item.Position)
            .FirstOrDefault();

        return new ProductReadModel(
            variant.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            product.Name,
            $"/product/{product.Slug}?variantId={variant.Id}",
            image?.ImagePath ?? FallbackImage,
            image?.AltText ?? product.Name,
            variant.Price,
            null,
            0,
            null,
            product.Description,
            string.Join(
                ' ',
                product.Slug,
                product.Name,
                product.Brand?.Name,
                product.Category?.Name,
                variant.Code,
                variant.ColorName),
            [product.Slug, variant.Code]);
    }
}
