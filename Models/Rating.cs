using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Rating
{
    public long Id { get; set; }

    public long OrderItemId { get; set; }

    public long UserId { get; set; }

    public int Stars { get; set; }

    public string? Comment { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
