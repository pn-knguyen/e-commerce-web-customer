using System.Globalization;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Wishlist;

public sealed class DbWishlistService(EcommerceDbContext dbContext) : IWishlistService
{
    public async Task<WishlistToggleResult> ToggleAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(userEmail, cancellationToken);
        if (user is null)
        {
            return new WishlistToggleResult(false, false, "Không xác định được tài khoản.");
        }

        var variant = await ResolveVariantAsync(productVariantKey, cancellationToken);
        if (variant is null)
        {
            return new WishlistToggleResult(false, false, "Sản phẩm không tồn tại hoặc đã ngừng bán.");
        }

        var existing = await dbContext.Wishlists.FirstOrDefaultAsync(
            item => item.UserId == user.Id && item.ProductVariantId == variant.Id,
            cancellationToken);

        if (existing is not null)
        {
            dbContext.Wishlists.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new WishlistToggleResult(true, false, "Đã bỏ sản phẩm khỏi danh sách yêu thích.");
        }

        dbContext.Wishlists.Add(new Models.Entities.Wishlist
        {
            UserId = user.Id,
            ProductVariantId = variant.Id,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WishlistToggleResult(true, true, "Đã thêm sản phẩm vào danh sách yêu thích.");
    }

    public async Task<WishlistActionResult> RemoveAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(userEmail, cancellationToken);
        if (user is null)
        {
            return new WishlistActionResult(false, "Không xác định được tài khoản.");
        }

        var variant = await ResolveVariantAsync(productVariantKey, cancellationToken);
        if (variant is null)
        {
            return new WishlistActionResult(false, "Sản phẩm không tồn tại hoặc đã ngừng bán.");
        }

        var existing = await dbContext.Wishlists.FirstOrDefaultAsync(
            item => item.UserId == user.Id && item.ProductVariantId == variant.Id,
            cancellationToken);
        if (existing is null)
        {
            return new WishlistActionResult(true, "Sản phẩm không còn trong danh sách yêu thích.");
        }

        dbContext.Wishlists.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new WishlistActionResult(true, "Đã bỏ sản phẩm khỏi danh sách yêu thích.");
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetStatusesAsync(
        string userEmail,
        IReadOnlyCollection<string> productVariantKeys,
        CancellationToken cancellationToken = default)
    {
        var keys = productVariantKeys
            .Select(key => key.Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();
        if (keys.Count == 0)
        {
            return new Dictionary<string, bool>();
        }

        var user = await ResolveUserAsync(userEmail, cancellationToken);
        if (user is null)
        {
            return keys.ToDictionary(key => key, _ => false, StringComparer.OrdinalIgnoreCase);
        }

        var numericIds = keys
            .Select(key => long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                ? id
                : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
        var codeKeys = keys
            .Where(key => !long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .ToList();

        var variants = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant => numericIds.Contains(variant.Id) || codeKeys.Contains(variant.Code))
            .Select(variant => new
            {
                variant.Id,
                variant.Code
            })
            .ToListAsync(cancellationToken);

        var variantIds = variants.Select(variant => variant.Id).ToList();
        var wishlistedVariantIds = await dbContext.Wishlists
            .AsNoTracking()
            .Where(item => item.UserId == user.Id && variantIds.Contains(item.ProductVariantId))
            .Select(item => item.ProductVariantId)
            .ToListAsync(cancellationToken);
        var wishlistedSet = wishlistedVariantIds.ToHashSet();

        return keys.ToDictionary(
            key => key,
            key =>
            {
                var variant = long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                    ? variants.FirstOrDefault(item => item.Id == id)
                    : variants.FirstOrDefault(item => string.Equals(item.Code, key, StringComparison.OrdinalIgnoreCase));
                return variant is not null && wishlistedSet.Contains(variant.Id);
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<User?> ResolveUserAsync(
        string userEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        return await dbContext.Users.FirstOrDefaultAsync(
            user => user.IsActive && user.Email.ToLower() == normalizedEmail,
            cancellationToken);
    }

    private async Task<ProductVariant?> ResolveVariantAsync(
        string productVariantKey,
        CancellationToken cancellationToken)
    {
        var key = productVariantKey.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var query = dbContext.ProductVariants
            .Include(variant => variant.Product)
            .Where(variant =>
                variant.IsActive
                && variant.Product != null
                && variant.Product.IsActive);

        if (long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var variantId))
        {
            return await query.FirstOrDefaultAsync(
                variant => variant.Id == variantId,
                cancellationToken);
        }

        return await query.FirstOrDefaultAsync(
            variant => variant.Code == key,
            cancellationToken);
    }
}
