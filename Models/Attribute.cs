using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Attribute
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<AttributeOption> AttributeOptions { get; set; } = new List<AttributeOption>();

    public virtual ICollection<CategoryVariantAttribute> CategoryVariantAttributes { get; set; } = new List<CategoryVariantAttribute>();
}
