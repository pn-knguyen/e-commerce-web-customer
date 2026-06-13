using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class ProductSpecification
{
    public long ProductId { get; set; }

    public long SpecificationId { get; set; }

    public string Value { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsHighlight { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Specification Specification { get; set; } = null!;
}
