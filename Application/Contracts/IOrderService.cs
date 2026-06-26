using e_commerce_web_customer.Application.Orders;

namespace e_commerce_web_customer.Application.Contracts;

public interface IOrderService
{
    Task<PlacedOrder> PlaceOrderAsync(
        PlaceOrderRequest request,
        bool clearCart = true,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentStatusAsync(
        string orderCode,
        bool isPaid,
        string transactionId,
        CancellationToken cancellationToken = default);

    Task CancelFailedOrderAsync(
        string orderCode,
        CancellationToken cancellationToken = default);

    Task ConfirmOnlinePaymentAsync(
        string orderCode,
        CancellationToken cancellationToken = default);

    Task<OrderForPayment?> GetOrderForPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default);
}
