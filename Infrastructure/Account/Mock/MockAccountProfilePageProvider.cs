using System.Globalization;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Infrastructure.Account.Mock;

public sealed class MockAccountProfilePageProvider : IAccountProfilePageProvider
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public Task<AccountProfilePageViewModel> GetProfilePageAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string activeTab,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var orders = CreateOrders();
        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? "0812345670" : phoneNumber.Trim();
        var summary = new AccountProfileSummaryViewModel
        {
            FullName = string.IsNullOrWhiteSpace(displayName) ? "Phạm Ngọc Khôi Nguyên" : displayName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? "demo@techstore.vn" : email.Trim(),
            PhoneNumber = normalizedPhone,
            MaskedPhoneNumber = MaskPhoneNumber(normalizedPhone),
            GenderText = "Nam",
            BirthDateText = "-",
            DefaultAddressText = "-",
            PasswordUpdatedAtText = "12/11/2023 12:42",
            OrderCountText = "34",
            TotalSpentText = FormatCurrency(9_829_000m)
        };

        return Task.FromResult(new AccountProfilePageViewModel
        {
            ActiveTab = AccountProfileTabs.Normalize(activeTab),
            Summary = summary,
            RecentOrders = orders.Take(3).ToList(),
            Orders = orders
        });
    }

    private static IReadOnlyList<AccountProfileOrderViewModel> CreateOrders() =>
    [
        Order("#WN0303995253", "20/12/2025", "USB 3.2 KINGSTON DATATRAVELER EXODIA DTXM 128GB", "/images/categories/accessories/memory-usb.webp", "USB Kingston DataTraveler Exodia", 349_000m, 377_000m, "Đã nhận hàng", "success", "Cùng 1 sản phẩm khác"),
        Order("#WN0303995250", "20/12/2025", "USB 3.2 Kingston DataTraveler Exodia DTX 128GB-Đen", "/images/categories/accessories/memory-usb.webp", "USB Kingston DataTraveler Exodia màu đen", 349_000m, 377_000m, "Chờ xác nhận", "pending", "Cùng 1 sản phẩm khác"),
        Order("#00142S2512000586", "13/12/2025", "USB 16GB SANDISK CZ600 3.0", "/images/categories/accessories/memory-usb.webp", "USB Sandisk CZ600", 229_000m, 154_000m, "Đã nhận hàng", "success", ""),
        Order("#WN0303855371", "22/11/2025", "KEY ĐIỆN TỬ - PHẦN MỀM MICROSOFT OFFICE 365 FAMILY (12 THÁNG X 6 USER)", "/images/products/computing/component-04.webp", "Microsoft Office 365 Family", 2_590_000m, 1_990_000m, "Đã nhận hàng", "success", ""),
        Order("#00142S2510001077", "20/10/2025", "SAMSUNG GALAXY A17 5G 8GB 128GB ĐEN (A176)", "/images/products/phone/phone-gaming-cutout.png", "Samsung Galaxy A17 5G màu đen", 6_190_000m, 5_891_000m, "Đã nhận hàng", "success", "Cùng 5 sản phẩm khác")
    ];

    private static AccountProfileOrderViewModel Order(
        string code,
        string date,
        string name,
        string imageUrl,
        string imageAlt,
        decimal price,
        decimal total,
        string status,
        string tone,
        string otherItemsText)
    {
        var items = new List<AccountProfileOrderItemViewModel>
        {
            new()
            {
                ProductName = name,
                ProductImageUrl = imageUrl,
                ProductImageAlt = imageAlt,
                VariantText = "",
                Quantity = 1,
                LineTotalText = FormatCurrency(price)
            }
        };

        if (!string.IsNullOrWhiteSpace(otherItemsText))
        {
            items.Add(new AccountProfileOrderItemViewModel
            {
                ProductName = "Phụ kiện đi kèm",
                ProductImageUrl = "/images/categories/accessories/memory-usb.webp",
                ProductImageAlt = "Phụ kiện TechStore",
                VariantText = "Gói mua thêm",
                Quantity = 1,
                LineTotalText = FormatCurrency(Math.Max(0m, total - price))
            });
        }

        return new AccountProfileOrderViewModel
        {
            OrderCode = code,
            OrderedDateText = date,
            Items = items,
            ProductName = name,
            ProductImageUrl = imageUrl,
            ProductImageAlt = imageAlt,
            ProductPriceText = FormatCurrency(price),
            TotalText = FormatCurrency(total),
            StatusText = status,
            StatusTone = tone,
            OtherItemsText = otherItemsText,
            DetailUrl = "/account/orders/" + Uri.EscapeDataString(code.TrimStart('#'))
        };
    }

    private static string FormatCurrency(decimal value) =>
        value.ToString("N0", ViCulture) + "đ";

    private static string MaskPhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digits.Length < 5 ? phoneNumber : $"{digits[..3]}*****{digits[^2..]}";
    }
}
