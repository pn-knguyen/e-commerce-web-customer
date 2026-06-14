using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class VoucherUser
{
    public long Id { get; set; }

    public long VoucherId { get; set; }

    public long UserId { get; set; }

    public int MaxUses { get; set; }

    public int UsedCount { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
