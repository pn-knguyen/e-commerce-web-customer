namespace e_commerce_web_customer.DTOs.Cart;

public class CartItemDto
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public long ProductVariantId { get; set; }

    public string ProductSlug { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string VariantCode { get; set; } = null!;

    public string? ColorName { get; set; }

    public string? ImagePath { get; set; }

    public int Quantity { get; set; }

    public int AvailableQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal LineTotal => Math.Max(0, UnitPrice - DiscountValue) * Quantity;
}
