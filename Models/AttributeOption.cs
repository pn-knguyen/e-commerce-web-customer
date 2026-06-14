using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class AttributeOption
{
    public long Id { get; set; }

    public long AttributeId { get; set; }

    public string Value { get; set; } = null!;

    public string Label { get; set; } = null!;

    public virtual Attribute Attribute { get; set; } = null!;

    public virtual ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
}
