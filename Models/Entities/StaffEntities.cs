namespace e_commerce_web_customer.Models.Entities;

public class Staff
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? AvatarImage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? NormalizedUserName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    public ICollection<StaffClaim> Claims { get; set; } = new List<StaffClaim>();
    public ICollection<StaffLogin> Logins { get; set; } = new List<StaffLogin>();
    public ICollection<StaffToken> Tokens { get; set; } = new List<StaffToken>();
    public ICollection<StaffUserRole> UserRoles { get; set; } = new List<StaffUserRole>();
    public ICollection<GoodsReceipt> CreatedGoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<GoodsReceipt> ApprovedGoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<Shipment> RequestedShipments { get; set; } = new List<Shipment>();
    public ICollection<CustomerConversation> AssignedCustomerConversations { get; set; } = new List<CustomerConversation>();
    public ICollection<CustomerMessage> CustomerMessages { get; set; } = new List<CustomerMessage>();
}

public class StaffClaim
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }

    public Staff? User { get; set; }
}

public class StaffLogin
{
    public string LoginProvider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
    public long UserId { get; set; }

    public Staff? User { get; set; }
}

public class StaffRole
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? ConcurrencyStamp { get; set; }

    public ICollection<StaffUserRole> UserRoles { get; set; } = new List<StaffUserRole>();
    public ICollection<StaffRoleClaim> RoleClaims { get; set; } = new List<StaffRoleClaim>();
}

public class StaffRoleClaim
{
    public int Id { get; set; }
    public long RoleId { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }

    public StaffRole? Role { get; set; }
}

public class StaffToken
{
    public long UserId { get; set; }
    public string LoginProvider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }

    public Staff? User { get; set; }
}

public class StaffUserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public Staff? User { get; set; }
    public StaffRole? Role { get; set; }
}
