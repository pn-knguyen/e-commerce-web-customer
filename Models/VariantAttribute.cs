using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class VariantAttribute
{
    public long ProductVariantId { get; set; }

    public long AttributeOptionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AttributeOption AttributeOption { get; set; } = null!;

    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
