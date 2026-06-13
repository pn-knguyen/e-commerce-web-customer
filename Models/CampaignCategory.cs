using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class CampaignCategory
{
    public long Id { get; set; }

    public long CampaignId { get; set; }

    public long CategoryId { get; set; }

    public int Position { get; set; }

    public string? ImagePath { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
