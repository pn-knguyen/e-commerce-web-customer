using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class ProductVariant
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string Code { get; set; } = null!;

    public decimal Price { get; set; }

    public int SoldCount { get; set; }

    public int Quantity { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ColorHex { get; set; }

    public string? ColorName { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<GoodReceiptItem> GoodReceiptItems { get; set; } = new List<GoodReceiptItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductVariantImage> ProductVariantImages { get; set; } = new List<ProductVariantImage>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

    public virtual ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
