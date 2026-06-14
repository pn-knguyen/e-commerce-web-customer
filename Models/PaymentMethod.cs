using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class PaymentMethod
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
