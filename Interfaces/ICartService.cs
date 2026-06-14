using e_commerce_web_customer.DTOs.Cart;

namespace e_commerce_web_customer.Interfaces;

public interface ICartService
{
    Task<List<CartItemDto>> GetAsync(long userId);

    Task<CartItemDto> AddAsync(long userId, long productVariantId, int quantity);

    Task<CartItemDto?> UpdateAsync(long userId, long cartItemId, int quantity);

    Task<bool> DeleteAsync(long userId, long cartItemId);
}
