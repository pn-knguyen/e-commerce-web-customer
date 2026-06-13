using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Campaign
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CampaignCategory> CampaignCategories { get; set; } = new List<CampaignCategory>();
}
