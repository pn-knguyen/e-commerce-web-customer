using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class ProductVariantImage
{
    public long Id { get; set; }

    public long ProductVariantId { get; set; }

    public string ImagePath { get; set; } = null!;

    public string? AltText { get; set; }

    public int Position { get; set; }

    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
