namespace e_commerce_web_customer.Application.Orders;

public sealed record PlaceOrderRequest(
    string UserEmail,
    string CustomerName,
    string Phone,
    string Email,
    long? ShippingAddressId,
    string ShippingProvince,
    string ShippingWard,
    string ShippingDetail,
    long PaymentMethodId,
    string? Note,
    decimal ShippingFee,
    decimal Discount,
    IReadOnlyList<PlaceOrderLine> Items);

public sealed record PlaceOrderLine(
    string ProductId,
    string Name,
    string Variant,
    decimal UnitPrice,
    int Quantity);

public sealed record PlacedOrder(
    string OrderCode,
    DateTimeOffset PlacedAt,
    DateTimeOffset EstimatedDeliveryAt);

public sealed record OrderForPayment(
    string OrderCode,
    decimal TotalAmount);
