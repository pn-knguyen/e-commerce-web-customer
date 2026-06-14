namespace e_commerce_web_customer.DTOs.Order;

public class OrderDto
{
    public long Id { get; set; }

    public string OrderCode { get; set; } = null!;

    public string PaymentMethodName { get; set; } = null!;

    public decimal SubtotalAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal VoucherDiscount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public long ProductVariantId { get; set; }

    public string ProductName { get; set; } = null!;

    public string VariantName { get; set; } = null!;

    public string? ImagePath { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
