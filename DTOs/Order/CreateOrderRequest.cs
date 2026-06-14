namespace e_commerce_web_customer.DTOs.Order;

public class CreateOrderRequest
{
    public long UserId { get; set; }

    public string ContactName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string DetailAddress { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public decimal ShippingFee { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public long ProductVariantId { get; set; }

    public int Quantity { get; set; }
}
