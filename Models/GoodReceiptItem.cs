using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class GoodReceiptItem
{
    public long Id { get; set; }

    public long GoodsReceiptId { get; set; }

    public long ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public decimal ImportPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
