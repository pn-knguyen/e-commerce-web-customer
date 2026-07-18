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
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var orderHistory = CreateOrderHistory(orderStatus, fromDate, toDate);
        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? "0812345670" : phoneNumber.Trim();
        var summary = new AccountProfileSummaryViewModel
        {
            FullName = string.IsNullOrWhiteSpace(displayName) ? "Phạm Ngọc Khôi Nguyên" : displayName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? "demo@techstore.vn" : email.Trim(),
            PhoneNumber = normalizedPhone,
            MaskedPhoneNumber = MaskPhoneNumber(normalizedPhone),
            GenderValue = "male",
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
            OrderFilter = new AccountProfileOrderFilterViewModel
            {
                Status = orderHistory.OrderFilter.Status,
                FromDate = orderHistory.OrderFilter.FromDate,
                ToDate = orderHistory.OrderFilter.ToDate,
                FromDateText = orderHistory.OrderFilter.FromDateText,
                ToDateText = orderHistory.OrderFilter.ToDateText
            },
            OrderStatusFilters = orderHistory.OrderStatusFilters,
            RecentOrders = orderHistory.Orders.Take(3).ToList(),
            Orders = orderHistory.Orders,
            Vouchers = CreateVouchers()
        });
    }

    public Task<AccountOrderHistoryViewModel> GetOrderHistoryAsync(
        string? email,
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateOrderHistory(orderStatus, fromDate, toDate));
    }

    private static AccountOrderHistoryViewModel CreateOrderHistory(
        string? orderStatus,
        string? fromDate,
        string? toDate)
    {
        var normalizedStatus = AccountOrderStatusFilterKeys.Normalize(orderStatus);
        var (fromDateValue, toDateValue) = ParseDateRange(fromDate, toDate);
        var datedOrders = ApplyDateFilter(CreateOrders(), fromDateValue, toDateValue);
        var orders = ApplyStatusFilter(datedOrders, normalizedStatus);

        return new AccountOrderHistoryViewModel
        {
            OrderFilter = new AccountProfileOrderFilterViewModel
            {
                Status = normalizedStatus,
                FromDate = FormatDateInput(fromDateValue),
                ToDate = FormatDateInput(toDateValue),
                FromDateText = fromDateValue?.ToString("dd/MM/yyyy", ViCulture) ?? "Từ ngày",
                ToDateText = toDateValue?.ToString("dd/MM/yyyy", ViCulture) ?? "Đến ngày"
            },
            OrderStatusFilters = CreateOrderStatusFilters(
                normalizedStatus,
                datedOrders,
                fromDateValue,
                toDateValue),
            Orders = orders
        };
    }

    private static IReadOnlyList<AccountProfileOrderViewModel> ApplyDateFilter(
        IReadOnlyList<AccountProfileOrderViewModel> orders,
        DateTime? fromDate,
        DateTime? toDate)
    {
        return orders
            .Where(order => DateTime.TryParseExact(
                order.OrderedDateText,
                "dd/MM/yyyy",
                ViCulture,
                DateTimeStyles.None,
                out var orderedDate)
                && (!fromDate.HasValue || orderedDate.Date >= fromDate.Value)
                && (!toDate.HasValue || orderedDate.Date <= toDate.Value))
            .ToList();
    }

    private static IReadOnlyList<AccountProfileOrderViewModel> ApplyStatusFilter(
        IReadOnlyList<AccountProfileOrderViewModel> orders,
        string status)
    {
        return status switch
        {
            AccountOrderStatusFilterKeys.Pending => orders
                .Where(order => string.Equals(order.StatusTone, "pending", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AccountOrderStatusFilterKeys.Processing => orders
                .Where(order => order.StatusText.Contains("Đang xử lý", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AccountOrderStatusFilterKeys.Shipping => orders
                .Where(order => order.StatusText.Contains("vận chuyển", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AccountOrderStatusFilterKeys.Completed => orders
                .Where(order => order.StatusText.Contains("nhận", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            AccountOrderStatusFilterKeys.Cancelled => orders
                .Where(order => order.StatusText.Contains("hủy", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            _ => orders
        };
    }

    private static IReadOnlyList<AccountProfileOrderStatusFilterViewModel> CreateOrderStatusFilters(
        string activeStatus,
        IReadOnlyList<AccountProfileOrderViewModel> orders,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var filters = new[]
        {
            (AccountOrderStatusFilterKeys.All, "Tất cả", orders.Count),
            (AccountOrderStatusFilterKeys.Pending, "Chờ xác nhận", CountStatus("Chờ xác nhận")),
            (AccountOrderStatusFilterKeys.Processing, "Đang xử lý", CountStatus("Đang xử lý")),
            (AccountOrderStatusFilterKeys.Shipping, "Đang vận chuyển", CountStatus("Đang vận chuyển")),
            (AccountOrderStatusFilterKeys.Completed, "Đã nhận hàng", CountStatus("Đã nhận hàng")),
            (AccountOrderStatusFilterKeys.Cancelled, "Đã hủy", CountStatus("Đã hủy"))
        };

        return filters
            .Select(item => new AccountProfileOrderStatusFilterViewModel
            {
                Key = item.Item1,
                Label = item.Item2,
                Count = item.Item3,
                Url = BuildProfileHistoryUrl(item.Item1, fromDate, toDate),
                IsActive = string.Equals(item.Item1, activeStatus, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        int CountStatus(string statusText) => orders.Count(order =>
            string.Equals(order.StatusText, statusText, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildProfileHistoryUrl(
        string status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = new List<string> { "tab=history" };
        if (!string.Equals(status, AccountOrderStatusFilterKeys.All, StringComparison.OrdinalIgnoreCase))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (fromDate.HasValue) query.Add($"from={FormatDateInput(fromDate)}");
        if (toDate.HasValue) query.Add($"to={FormatDateInput(toDate)}");
        return "/Account/Profile?" + string.Join('&', query);
    }

    private static (DateTime? FromDate, DateTime? ToDate) ParseDateRange(
        string? fromDate,
        string? toDate)
    {
        var fromDateValue = ParseDate(fromDate);
        var toDateValue = ParseDate(toDate);
        if (fromDateValue.HasValue
            && toDateValue.HasValue
            && fromDateValue.Value > toDateValue.Value)
        {
            (fromDateValue, toDateValue) = (toDateValue, fromDateValue);
        }

        return (fromDateValue, toDateValue);
    }

    private static DateTime? ParseDate(string? value)
    {
        return DateTime.TryParseExact(
            value?.Trim(),
            ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"],
            ViCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.Date
            : null;
    }

    private static string FormatDateInput(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static IReadOnlyList<AccountProfileVoucherViewModel> CreateVouchers() =>
    [
        new()
        {
            Id = 1,
            Code = "WELCOME2026",
            Title = "Giảm 100.000đ",
            Description = "Ưu đãi chào mừng thành viên TechStore.",
            DiscountText = "Giảm 100.000đ",
            ConditionText = "Đơn từ 2.000.000đ",
            ExpiryText = "HSD 31/12/2026",
            UsageText = "Còn 1/1 lượt",
            Tone = "private",
            IsAvailable = true,
            IsAssignedToUser = true
        }
    ];

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
