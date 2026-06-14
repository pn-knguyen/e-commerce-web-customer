using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Data;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbCartItemValidator(EcommerceDbContext dbContext) : ICartItemValidator
{
    public async Task<CartSessionItem> ValidateAsync(
        CartSessionItem requestItem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestItem.Id) &&
            (!requestItem.ProductVariantId.HasValue || requestItem.ProductVariantId <= 0))
        {
            throw new CartItemValidationException("Product is invalid.");
        }

        var hasVariantId = requestItem.ProductVariantId is > 0;
        var parsedVariantId = long.TryParse(requestItem.Id, out var idFromText)
            ? idFromText
            : 0;
        var requestedVariantId = hasVariantId
            ? requestItem.ProductVariantId!.Value
            : parsedVariantId;
        var requestedSlug = requestItem.Id.Trim();

        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(item => item.Product)
            .Include(item => item.ProductVariantImages)
            .Where(item => item.IsActive && item.Product.IsActive)
            .Where(item =>
                requestedVariantId > 0
                    ? item.Id == requestedVariantId
                    : item.Product.Slug == requestedSlug)
            .OrderByDescending(item =>
                !string.IsNullOrWhiteSpace(requestItem.Variant) &&
                item.ColorName == requestItem.Variant)
            .ThenByDescending(item => item.IsDefault)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (variant is null)
        {
            throw new CartItemValidationException("Product is unavailable.");
        }

        var quantity = Math.Clamp(requestItem.Quantity, 1, 10);
        if (variant.Quantity > 0)
        {
            quantity = Math.Min(quantity, variant.Quantity);
        }

        var image = variant.ProductVariantImages
            .OrderBy(item => item.Position)
            .FirstOrDefault();

        return new CartSessionItem
        {
            ProductVariantId = variant.Id,
            Id = variant.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Name = variant.Product.Name,
            ProductUrl = $"/product/{variant.Product.Slug}?variantId={variant.Id}",
            ImageUrl = image?.ImagePath ?? "/images/logo-techstore-icon.svg",
            ImageAlt = image?.AltText ?? variant.Product.Name,
            Variant = variant.ColorName ?? variant.Code,
            UnitPrice = variant.Price,
            Quantity = quantity
        };
    }
}
