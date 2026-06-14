namespace e_commerce_web_customer.Application.Orders;

public sealed class OrderPlacementException(string message) : Exception(message);
