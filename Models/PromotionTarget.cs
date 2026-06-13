using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class PromotionTarget
{
    public long Id { get; set; }

    public long PromotionId { get; set; }

    public string TargetType { get; set; } = null!;

    public long TargetId { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
