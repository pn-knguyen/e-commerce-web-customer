namespace e_commerce_web_customer.Application.Services;

public sealed class CartItemValidationException(string message) : Exception(message);
