using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class PromotionRule
{
    public long Id { get; set; }

    public long PromotionId { get; set; }

    public long? GiftProductVariantId { get; set; }

    public string ActionType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public int BuyQuantity { get; set; }

    public int GetQuantity { get; set; }

    public virtual ProductVariant? GiftProductVariant { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
