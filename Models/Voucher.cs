using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Voucher
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal MinOrderValue { get; set; }

    public decimal? MaxDiscountValue { get; set; }

    public int? MaxUses { get; set; }

    public int? MaxUsesPerUser { get; set; }

    public int UsedCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<VoucherTarget> VoucherTargets { get; set; } = new List<VoucherTarget>();

    public virtual ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();

    public virtual ICollection<VoucherUser> VoucherUsers { get; set; } = new List<VoucherUser>();
}
