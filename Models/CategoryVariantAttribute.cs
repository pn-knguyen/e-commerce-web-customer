using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class CategoryVariantAttribute
{
    public long AttributeId { get; set; }

    public long CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Attribute Attribute { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
