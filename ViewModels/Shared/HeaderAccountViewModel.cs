namespace e_commerce_web_customer.ViewModels.Shared;

public sealed class HeaderAccountViewModel
{
    public bool IsLoggedIn { get; init; }
    public string DisplayName { get; init; } = "Đăng nhập";
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string MemberUrl { get; init; } = "/member";
    public string MemberLogoUrl { get; init; } = "/images/logo-techstore-icon.svg";
    public string ReadStatusText { get; init; } = "Bạn đã đọc tất cả thông báo ✓✓";
    public IReadOnlyList<HeaderAccountNotificationViewModel> Notifications { get; init; } = [];

    public bool HasNotifications => Notifications.Count > 0;
}

public sealed class HeaderAccountNotificationViewModel
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string LeadText { get; init; }
    public required string HighlightText { get; init; }
    public required string TailText { get; init; }
    public required string TimeAgoText { get; init; }
    public required string DetailUrl { get; init; }
    public bool IsRead { get; init; } = true;
}
