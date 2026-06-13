using e_commerce_web_customer.DTOs.Order;

namespace e_commerce_web_customer.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request);

    Task<OrderDto?> GetByIdAsync(long userId, long orderId);

    Task<List<OrderDto>> GetByUserAsync(long userId);
}
