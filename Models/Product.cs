using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Product
{
    public long Id { get; set; }

    public long BrandId { get; set; }

    public long CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public int ViewsCount { get; set; }

    public int TotalSoldCount { get; set; }

    public decimal RatingAverage { get; set; }

    public int RatingCount { get; set; }

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
