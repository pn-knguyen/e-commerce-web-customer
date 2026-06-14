using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Category
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public long? ParentId { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public string Slug { get; set; } = null!;

    public int Position { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CampaignCategory> CampaignCategories { get; set; } = new List<CampaignCategory>();

    public virtual ICollection<CategorySpecification> CategorySpecifications { get; set; } = new List<CategorySpecification>();

    public virtual ICollection<CategoryVariantAttribute> CategoryVariantAttributes { get; set; } = new List<CategoryVariantAttribute>();

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual Category? Parent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
