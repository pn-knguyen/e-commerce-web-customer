using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class VoucherTarget
{
    public long Id { get; set; }

    public long VoucherId { get; set; }

    public string TargetType { get; set; } = null!;

    public long TargetId { get; set; }

    public virtual Voucher Voucher { get; set; } = null!;
}
