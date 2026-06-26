namespace e_commerce_web_customer.ViewModels.Account;

public sealed class AccountProfilePageViewModel
{
    public string ActiveTab { get; init; } = AccountProfileTabs.Overview;
    public required AccountProfileSummaryViewModel Summary { get; init; }
    public IReadOnlyList<AccountProfileOrderViewModel> RecentOrders { get; init; } = [];
    public IReadOnlyList<AccountProfileOrderViewModel> Orders { get; init; } = [];
    public IReadOnlyList<AccountProfileAddressViewModel> Addresses { get; init; } = [];
    public IReadOnlyList<AccountProfileLinkedAccountViewModel> LinkedAccounts { get; init; } =
    [
        new() { ProviderName = "Google", LogoText = "G", LogoClass = "google" },
        new() { ProviderName = "Zalo", LogoText = "Zalo", LogoClass = "zalo" }
    ];

    public bool IsOverview => string.Equals(ActiveTab, AccountProfileTabs.Overview, StringComparison.OrdinalIgnoreCase);
    public bool IsHistory => string.Equals(ActiveTab, AccountProfileTabs.History, StringComparison.OrdinalIgnoreCase);
    public bool IsInfo => string.Equals(ActiveTab, AccountProfileTabs.Info, StringComparison.OrdinalIgnoreCase);
    public bool HasAddresses => Addresses.Count > 0;
}

public static class AccountProfileTabs
{
    public const string Overview = "overview";
    public const string History = "history";
    public const string Info = "info";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            History => History,
            Info => Info,
            _ => Overview
        };
    }
}

public sealed class AccountProfileSummaryViewModel
{
    public string FullName { get; init; } = "Thành viên TechStore";
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string MaskedPhoneNumber { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = "/images/logo-techstore-icon.svg";
    public string GenderText { get; init; } = "-";
    public string BirthDateText { get; init; } = "-";
    public string DefaultAddressText { get; init; } = "-";
    public string PasswordUpdatedAtText { get; init; } = "-";
    public string RefreshDateText { get; init; } = "Cập nhật lại sau 01/01/2027";
    public string OrderCountText { get; init; } = "0";
    public string TotalSpentText { get; init; } = "0đ";
    public string AccumulationDateText { get; init; } = "Từ 01/01/2025";
}

public sealed class AccountProfileAddressViewModel
{
    public long Id { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string AddressText { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public sealed class AccountProfileLinkedAccountViewModel
{
    public string ProviderName { get; init; } = string.Empty;
    public string LogoText { get; init; } = string.Empty;
    public string LogoClass { get; init; } = string.Empty;
}

public sealed class AccountProfileOrderViewModel
{
    public string OrderCode { get; init; } = string.Empty;
    public string OrderedDateText { get; init; } = string.Empty;
    public IReadOnlyList<AccountProfileOrderItemViewModel> Items { get; init; } = [];
    public string ProductName { get; init; } = string.Empty;
    public string ProductImageUrl { get; init; } = "/images/logo-techstore-icon.svg";
    public string ProductImageAlt { get; init; } = string.Empty;
    public string ProductPriceText { get; init; } = string.Empty;
    public string OtherItemsText { get; init; } = string.Empty;
    public string TotalText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string StatusTone { get; init; } = "success";
    public string DetailUrl { get; init; } = "#";
}

public sealed class AccountProfileOrderItemViewModel
{
    public string ProductName { get; init; } = string.Empty;
    public string ProductImageUrl { get; init; } = "/images/logo-techstore-icon.svg";
    public string ProductImageAlt { get; init; } = string.Empty;
    public string VariantText { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public string LineTotalText { get; init; } = string.Empty;
}
