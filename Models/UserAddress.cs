using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class UserAddress
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string ContactName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string ProvinceCode { get; set; } = null!;

    public string ProvinceName { get; set; } = null!;

    public string WardCode { get; set; } = null!;

    public string WardName { get; set; } = null!;

    public string DetailAddress { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
