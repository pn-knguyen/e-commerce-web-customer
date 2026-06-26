using System.Collections.Concurrent;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Orders;

namespace e_commerce_web_customer.Infrastructure.Orders.Mock;

public sealed class MockOrderService : IOrderService
{
    private readonly ConcurrentDictionary<string, PlaceOrderRequest> _orders = new();

    public Task<PlacedOrder> PlaceOrderAsync(
        PlaceOrderRequest request,
        bool clearCart = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Items.Count == 0)
        {
            throw new OrderPlacementException("Đơn hàng không có sản phẩm.");
        }

        var placedAt = DateTimeOffset.Now;
        var orderCode = $"TS{placedAt:yyyyMMddHHmmss}{Random.Shared.Next(100, 1000)}";
        _orders[orderCode] = request;

        return Task.FromResult(new PlacedOrder(
            orderCode,
            placedAt,
            placedAt.AddDays(2)));
    }

    public Task UpdatePaymentStatusAsync(
        string orderCode,
        bool isPaid,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        // Mock: no persistent storage — status update is a no-op
        return Task.CompletedTask;
    }

    public Task<OrderForPayment?> GetOrderForPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (_orders.TryGetValue(orderCode, out var request) && request.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
        {
            var subtotal = request.Items.Sum(item => item.UnitPrice * item.Quantity);
            var total = Math.Max(0m, subtotal + request.ShippingFee - request.Discount);
            return Task.FromResult<OrderForPayment?>(new OrderForPayment(orderCode, total));
        }

        return Task.FromResult<OrderForPayment?>(null);
    }

    public Task CancelFailedOrderAsync(
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ConfirmOnlinePaymentAsync(
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
