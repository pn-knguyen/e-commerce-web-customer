using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string Gender { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? AvatarImage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<GoodsReceipt> GoodsReceiptApprovedByNavigations { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<GoodsReceipt> GoodsReceiptCreatedByNavigations { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    public virtual ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();

    public virtual ICollection<VoucherUser> VoucherUsers { get; set; } = new List<VoucherUser>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
