using System.Linq.Expressions;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.DTOs.Cart;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Services;

public class CartService : ICartService
{
    private static readonly Expression<Func<CartItem, CartItemDto>> CartItemProjection = item =>
        new CartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductVariant.ProductId,
            ProductVariantId = item.ProductVariantId,
            ProductSlug = item.ProductVariant.Product.Slug,
            ProductName = item.ProductVariant.Product.Name,
            VariantCode = item.ProductVariant.Code,
            ColorName = item.ProductVariant.ColorName,
            ImagePath = item.ProductVariant.ProductVariantImages
                .OrderBy(image => image.Position)
                .Select(image => image.ImagePath)
                .FirstOrDefault(),
            Quantity = item.Quantity,
            AvailableQuantity = item.ProductVariant.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountValue = item.DiscountValue
        };

    private readonly EcommerceDbContext _dbContext;

    public CartService(EcommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CartItemDto>> GetAsync(long userId)
    {
        return await CartItemsQuery(userId)
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Select(CartItemProjection)
            .ToListAsync();
    }

    public async Task<CartItemDto> AddAsync(long userId, long productVariantId, int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (!await _dbContext.Users.AnyAsync(user => user.Id == userId))
        {
            throw new KeyNotFoundException("User was not found.");
        }

        var variant = await _dbContext.ProductVariants
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == productVariantId);

        if (variant is null || !variant.IsActive || !variant.Product.IsActive)
        {
            throw new KeyNotFoundException("Product variant was not found.");
        }

        var cartItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(item =>
                item.UserId == userId &&
                item.ProductVariantId == productVariantId);
        var newQuantity = (cartItem?.Quantity ?? 0) + quantity;

        ValidateStock(newQuantity, variant.Quantity);

        if (cartItem is null)
        {
            cartItem = new CartItem
            {
                UserId = userId,
                ProductVariantId = productVariantId,
                Quantity = newQuantity,
                UnitPrice = variant.Price,
                DiscountValue = 0,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.CartItems.Add(cartItem);
        }
        else
        {
            cartItem.Quantity = newQuantity;
            cartItem.UnitPrice = variant.Price;
            cartItem.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return await GetItemAsync(userId, cartItem.Id)
            ?? throw new InvalidOperationException("Cart item could not be loaded after it was added.");
    }

    public async Task<CartItemDto?> UpdateAsync(long userId, long cartItemId, int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var cartItem = await _dbContext.CartItems
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant.Product)
            .FirstOrDefaultAsync(item => item.Id == cartItemId && item.UserId == userId);

        if (cartItem is null)
        {
            return null;
        }

        if (!cartItem.ProductVariant.IsActive || !cartItem.ProductVariant.Product.IsActive)
        {
            throw new InvalidOperationException("Product variant is no longer available.");
        }

        ValidateStock(quantity, cartItem.ProductVariant.Quantity);

        cartItem.Quantity = quantity;
        cartItem.UnitPrice = cartItem.ProductVariant.Price;
        cartItem.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetItemAsync(userId, cartItem.Id);
    }

    public async Task<bool> DeleteAsync(long userId, long cartItemId)
    {
        var cartItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(item => item.Id == cartItemId && item.UserId == userId);

        if (cartItem is null)
        {
            return false;
        }

        _dbContext.CartItems.Remove(cartItem);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    private Task<CartItemDto?> GetItemAsync(long userId, long cartItemId)
    {
        return CartItemsQuery(userId)
            .Where(item => item.Id == cartItemId)
            .Select(CartItemProjection)
            .FirstOrDefaultAsync();
    }

    private IQueryable<CartItem> CartItemsQuery(long userId)
    {
        return _dbContext.CartItems
            .AsNoTracking()
            .Where(item => item.UserId == userId);
    }

    private static void ValidateStock(int requestedQuantity, int availableQuantity)
    {
        if (availableQuantity > 0 && requestedQuantity > availableQuantity)
        {
            throw new InvalidOperationException(
                $"Only {availableQuantity} item(s) are currently available.");
        }
    }
}
