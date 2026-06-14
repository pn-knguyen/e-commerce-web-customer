using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class CartItem
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
