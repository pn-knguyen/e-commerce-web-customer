using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class CategorySpecification
{
    public long SpecificationId { get; set; }

    public long CategoryId { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }

    public string? GroupName { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Specification Specification { get; set; } = null!;
}
