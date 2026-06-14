using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Specification
{
    public long Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Unit { get; set; }

    public string? Icon { get; set; }

    public virtual ICollection<CategorySpecification> CategorySpecifications { get; set; } = new List<CategorySpecification>();

    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
}
