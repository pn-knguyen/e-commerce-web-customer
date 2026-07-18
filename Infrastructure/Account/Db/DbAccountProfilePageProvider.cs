using System.Globalization;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Account.Db;

public sealed class DbAccountProfilePageProvider(EcommerceDbContext dbContext) : IAccountProfilePageProvider
{
    private const string FallbackImage = "/images/logo-techstore-icon.svg";
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public async Task<AccountProfilePageViewModel> GetProfilePageAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string activeTab,
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedActiveTab = AccountProfileTabs.Normalize(activeTab);
        var normalizedOrderStatus = AccountOrderStatusFilterKeys.Normalize(orderStatus);
        var (fromDateValue, toDateValue) = ParseDateRange(fromDate, toDate);

        var user = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Email == normalizedEmail,
                    cancellationToken);

        if (user is null)
        {
            return CreateFallbackPage(
                normalizedEmail,
                displayName,
                phoneNumber,
                normalizedActiveTab,
                normalizedOrderStatus,
                fromDateValue,
                toDateValue);
        }

        var summaryData = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == user.Id)
            .Where(order => order.OrderStatus != OrderStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Total = group.Sum(order => order.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var orderHistory = string.Equals(
            normalizedActiveTab,
            AccountProfileTabs.Info,
            StringComparison.OrdinalIgnoreCase)
            ? CreateEmptyOrderHistory(normalizedOrderStatus, fromDateValue, toDateValue)
            : await LoadOrderHistoryAsync(
                user.Id,
                normalizedOrderStatus,
                fromDateValue,
                toDateValue,
                string.Equals(
                    normalizedActiveTab,
                    AccountProfileTabs.Overview,
                    StringComparison.OrdinalIgnoreCase)
                    ? 3
                    : 20,
                cancellationToken);

        IReadOnlyList<UserAddress> addresses = [];
        if (!string.Equals(normalizedActiveTab, AccountProfileTabs.History, StringComparison.OrdinalIgnoreCase))
        {
            addresses = await dbContext.UserAddresses
                .AsNoTracking()
                .Where(address => address.UserId == user.Id && !address.IsDeleted)
                .OrderByDescending(address => address.IsDefault)
                .ThenByDescending(address => address.UpdatedAt ?? address.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        IReadOnlyList<Models.Entities.Wishlist> favoriteProducts = [];
        IReadOnlyList<AccountProfileVoucherViewModel> vouchers = [];
        if (string.Equals(normalizedActiveTab, AccountProfileTabs.Overview, StringComparison.OrdinalIgnoreCase))
        {
            favoriteProducts = await dbContext.Wishlists
                .AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.ProductVariantImages)
                .Include(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.VariantAttributes)
                        .ThenInclude(attribute => attribute.AttributeOption)
                            .ThenInclude(option => option!.Attribute)
                .Where(item => item.UserId == user.Id)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
            vouchers = await GetProfileVouchersAsync(user.Id, cancellationToken);
        }

        var addressItems = addresses.Select(ToAddressViewModel).ToList();
        var favoriteItems = favoriteProducts
            .Select(ToFavoriteProductViewModel)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
        var defaultAddress = addressItems.FirstOrDefault(address => address.IsDefault)
            ?? addressItems.FirstOrDefault();
        var resolvedPhone = string.IsNullOrWhiteSpace(user.Phone)
            ? phoneNumber?.Trim() ?? string.Empty
            : user.Phone.Trim();

        var summary = new AccountProfileSummaryViewModel
        {
            FullName = string.IsNullOrWhiteSpace(user.FullName)
                ? displayName?.Trim() ?? user.Username
                : user.FullName.Trim(),
            Email = user.Email,
            PhoneNumber = resolvedPhone,
            MaskedPhoneNumber = MaskPhoneNumber(resolvedPhone),
            AvatarUrl = NormalizeImageUrl(user.AvatarImage),
            GenderValue = GetGenderValue(user.Gender),
            GenderText = GetGenderText(user.Gender),
            DefaultAddressText = defaultAddress?.AddressText ?? "-",
            PasswordUpdatedAtText = FormatDateTime(user.UpdatedAt ?? user.CreatedAt),
            OrderCountText = (summaryData?.Count ?? 0).ToString("N0", ViCulture),
            TotalSpentText = FormatCurrency(summaryData?.Total ?? 0m)
        };

        return new AccountProfilePageViewModel
        {
            ActiveTab = normalizedActiveTab,
            Summary = summary,
            OrderFilter = orderHistory.OrderFilter,
            OrderStatusFilters = orderHistory.OrderStatusFilters,
            RecentOrders = orderHistory.Orders.Take(3).ToList(),
            Orders = orderHistory.Orders,
            Addresses = addressItems,
            Vouchers = vouchers,
            FavoriteProducts = favoriteItems
        };
    }

    public async Task<AccountOrderHistoryViewModel> GetOrderHistoryAsync(
        string? email,
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedStatus = AccountOrderStatusFilterKeys.Normalize(orderStatus);
        var (fromDateValue, toDateValue) = ParseDateRange(fromDate, toDate);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return CreateEmptyOrderHistory(normalizedStatus, fromDateValue, toDateValue);
        }

        var userId = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email == normalizedEmail)
            .Select(user => (long?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return userId.HasValue
            ? await LoadOrderHistoryAsync(
                userId.Value,
                normalizedStatus,
                fromDateValue,
                toDateValue,
                20,
                cancellationToken)
            : CreateEmptyOrderHistory(normalizedStatus, fromDateValue, toDateValue);
    }

    private async Task<AccountOrderHistoryViewModel> LoadOrderHistoryAsync(
        long userId,
        string orderStatus,
        DateTime? fromDate,
        DateTime? toDate,
        int take,
        CancellationToken cancellationToken)
    {
        var ordersByAccount = ApplyOrderDateFilter(
            dbContext.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId),
            fromDate,
            toDate);
        var statusCounts = await ordersByAccount
            .GroupBy(order => order.OrderStatus)
            .Select(group => new OrderStatusCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var orders = await ApplyOrderStatusFilter(ordersByAccount, orderStatus)
            .AsSingleQuery()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.ProductVariantImages
                        .OrderBy(image => image.Position)
                        .ThenBy(image => image.Id)
                        .Take(1))
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.VariantAttributes)
                        .ThenInclude(item => item.AttributeOption)
                            .ThenInclude(option => option!.Attribute)
            .OrderByDescending(order => order.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new AccountOrderHistoryViewModel
        {
            OrderFilter = BuildOrderFilter(orderStatus, fromDate, toDate),
            OrderStatusFilters = BuildOrderStatusFilters(orderStatus, fromDate, toDate, statusCounts),
            Orders = orders.Select(ToOrderViewModel).ToList()
        };
    }

    private static AccountOrderHistoryViewModel CreateEmptyOrderHistory(
        string orderStatus,
        DateTime? fromDate,
        DateTime? toDate) => new()
    {
        OrderFilter = BuildOrderFilter(orderStatus, fromDate, toDate),
        OrderStatusFilters = BuildOrderStatusFilters(orderStatus, fromDate, toDate, []),
        Orders = []
    };

    private static AccountProfilePageViewModel CreateFallbackPage(
        string email,
        string? displayName,
        string? phoneNumber,
        string activeTab,
        string orderStatus,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var resolvedPhone = phoneNumber?.Trim() ?? string.Empty;
        return new AccountProfilePageViewModel
        {
            ActiveTab = AccountProfileTabs.Normalize(activeTab),
            OrderFilter = BuildOrderFilter(orderStatus, fromDate, toDate),
            OrderStatusFilters = BuildOrderStatusFilters(
                orderStatus,
                fromDate,
                toDate,
                []),
            Summary = new AccountProfileSummaryViewModel
            {
                FullName = string.IsNullOrWhiteSpace(displayName) ? "Thành viên TechStore" : displayName.Trim(),
                Email = email,
                PhoneNumber = resolvedPhone,
                MaskedPhoneNumber = MaskPhoneNumber(resolvedPhone),
                PasswordUpdatedAtText = "-"
            }
        };
    }

    private async Task<IReadOnlyList<AccountProfileVoucherViewModel>> GetProfileVouchersAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var vouchers = await dbContext.Vouchers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(voucher => voucher.VoucherUsers)
            .Include(voucher => voucher.VoucherUsages)
            .Include(voucher => voucher.VoucherTargets)
            .Where(voucher => voucher.IsActive)
            .Where(voucher => voucher.StartDate <= now && voucher.EndDate >= now)
            .Where(voucher => !voucher.MaxUses.HasValue || voucher.UsedCount < voucher.MaxUses.Value)
            .Where(voucher => voucher.VoucherUsers.Count == 0
                || voucher.VoucherUsers.Any(item => item.UserId == userId))
            .OrderByDescending(voucher => voucher.VoucherUsers.Any(item => item.UserId == userId))
            .ThenByDescending(voucher => voucher.Priority)
            .ThenBy(voucher => voucher.EndDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        return vouchers
            .Select(voucher => ToVoucherViewModel(voucher, userId, now))
            .ToList();
    }

    private static AccountProfileVoucherViewModel ToVoucherViewModel(
        Voucher voucher,
        long userId,
        DateTime now)
    {
        var assignedVoucher = voucher.VoucherUsers.FirstOrDefault(item => item.UserId == userId);
        var userUsageCount = Math.Max(
            assignedVoucher?.UsedCount ?? 0,
            voucher.VoucherUsages.Count(item => item.UserId == userId));
        var perUserLimit = assignedVoucher?.MaxUses ?? voucher.MaxUsesPerUser;
        var remainingUses = perUserLimit.HasValue
            ? Math.Max(0, perUserLimit.Value - userUsageCount)
            : (int?)null;
        var isAvailable = voucher.IsActive
            && voucher.StartDate <= now
            && voucher.EndDate >= now
            && (!voucher.MaxUses.HasValue || voucher.UsedCount < voucher.MaxUses.Value)
            && (!perUserLimit.HasValue || remainingUses > 0);
        var discountText = FormatVoucherDiscount(voucher);

        return new AccountProfileVoucherViewModel
        {
            Id = voucher.Id,
            Code = voucher.Code,
            Title = discountText,
            Description = string.IsNullOrWhiteSpace(voucher.Description)
                ? "Ưu đãi dành cho đơn hàng TechStore của bạn."
                : voucher.Description.Trim(),
            DiscountText = discountText,
            ConditionText = BuildVoucherConditionText(voucher),
            ExpiryText = $"HSD {voucher.EndDate.ToLocalTime():dd/MM/yyyy}",
            UsageText = BuildVoucherUsageText(remainingUses, perUserLimit, assignedVoucher is not null),
            Tone = !isAvailable
                ? "used"
                : assignedVoucher is not null
                    ? "private"
                    : "available",
            IsAvailable = isAvailable,
            IsAssignedToUser = assignedVoucher is not null
        };
    }

    private static AccountProfileOrderFilterViewModel BuildOrderFilter(
        string orderStatus,
        DateTime? fromDate,
        DateTime? toDate)
    {
        return new AccountProfileOrderFilterViewModel
        {
            Status = AccountOrderStatusFilterKeys.Normalize(orderStatus),
            FromDate = FormatDateInput(fromDate),
            ToDate = FormatDateInput(toDate),
            FromDateText = fromDate.HasValue ? FormatDate(fromDate.Value) : "Từ ngày",
            ToDateText = toDate.HasValue ? FormatDate(toDate.Value) : "Đến ngày"
        };
    }

    private static IReadOnlyList<AccountProfileOrderStatusFilterViewModel> BuildOrderStatusFilters(
        string activeStatus,
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<OrderStatusCount> statusCounts)
    {
        var normalizedStatus = AccountOrderStatusFilterKeys.Normalize(activeStatus);
        var allCount = statusCounts.Sum(item => item.Count);

        return
        [
            OrderStatusFilter(AccountOrderStatusFilterKeys.All, "Tất cả", allCount),
            OrderStatusFilter(
                AccountOrderStatusFilterKeys.Pending,
                "Chờ xác nhận",
                CountStatuses(statusCounts, OrderStatus.Pending)),
            OrderStatusFilter(
                AccountOrderStatusFilterKeys.Processing,
                "Đang xử lý",
                CountStatuses(statusCounts, OrderStatus.Confirmed, OrderStatus.Processing)),
            OrderStatusFilter(
                AccountOrderStatusFilterKeys.Shipping,
                "Đang vận chuyển",
                CountStatuses(statusCounts, OrderStatus.Shipping)),
            OrderStatusFilter(
                AccountOrderStatusFilterKeys.Completed,
                "Đã nhận hàng",
                CountStatuses(statusCounts, OrderStatus.Completed)),
            OrderStatusFilter(
                AccountOrderStatusFilterKeys.Cancelled,
                "Đã hủy",
                CountStatuses(statusCounts, OrderStatus.Cancelled, OrderStatus.Returned))
        ];

        AccountProfileOrderStatusFilterViewModel OrderStatusFilter(
            string key,
            string label,
            int count)
        {
            return new AccountProfileOrderStatusFilterViewModel
            {
                Key = key,
                Label = label,
                Count = count,
                Url = BuildProfileHistoryUrl(key, fromDate, toDate),
                IsActive = string.Equals(key, normalizedStatus, StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    private static IQueryable<Order> ApplyOrderDateFilter(
        IQueryable<Order> query,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue)
        {
            var fromUtc = ToVietnamDateStartUtc(fromDate.Value);
            query = query.Where(order => order.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toExclusiveUtc = ToVietnamDateStartUtc(toDate.Value.AddDays(1));
            query = query.Where(order => order.CreatedAt < toExclusiveUtc);
        }

        return query;
    }

    private static IQueryable<Order> ApplyOrderStatusFilter(
        IQueryable<Order> query,
        string orderStatus)
    {
        return AccountOrderStatusFilterKeys.Normalize(orderStatus) switch
        {
            AccountOrderStatusFilterKeys.Pending => query.Where(order =>
                order.OrderStatus == OrderStatus.Pending),
            AccountOrderStatusFilterKeys.Processing => query.Where(order =>
                order.OrderStatus == OrderStatus.Confirmed
                || order.OrderStatus == OrderStatus.Processing),
            AccountOrderStatusFilterKeys.Shipping => query.Where(order =>
                order.OrderStatus == OrderStatus.Shipping),
            AccountOrderStatusFilterKeys.Completed => query.Where(order =>
                order.OrderStatus == OrderStatus.Completed),
            AccountOrderStatusFilterKeys.Cancelled => query.Where(order =>
                order.OrderStatus == OrderStatus.Cancelled
                || order.OrderStatus == OrderStatus.Returned),
            _ => query
        };
    }

    private static int CountStatuses(
        IReadOnlyList<OrderStatusCount> statusCounts,
        params OrderStatus[] statuses)
    {
        return statusCounts
            .Where(item => statuses.Contains(item.Status))
            .Sum(item => item.Count);
    }

    private static string BuildProfileHistoryUrl(
        string status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = new List<string> { "tab=history" };
        var normalizedStatus = AccountOrderStatusFilterKeys.Normalize(status);
        if (!string.Equals(normalizedStatus, AccountOrderStatusFilterKeys.All, StringComparison.OrdinalIgnoreCase))
        {
            query.Add($"status={Uri.EscapeDataString(normalizedStatus)}");
        }

        if (fromDate.HasValue)
        {
            query.Add($"from={Uri.EscapeDataString(FormatDateInput(fromDate))}");
        }

        if (toDate.HasValue)
        {
            query.Add($"to={Uri.EscapeDataString(FormatDateInput(toDate))}");
        }

        return "/Account/Profile?" + string.Join('&', query);
    }

    private static DateTime? ParseDateFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };
        return DateTime.TryParseExact(
            value.Trim(),
            formats,
            ViCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.Date
            : null;
    }

    private static (DateTime? FromDate, DateTime? ToDate) ParseDateRange(
        string? fromDate,
        string? toDate)
    {
        var fromDateValue = ParseDateFilter(fromDate);
        var toDateValue = ParseDateFilter(toDate);
        if (fromDateValue.HasValue
            && toDateValue.HasValue
            && fromDateValue.Value > toDateValue.Value)
        {
            (fromDateValue, toDateValue) = (toDateValue, fromDateValue);
        }

        return (fromDateValue, toDateValue);
    }

    private static AccountProfileAddressViewModel ToAddressViewModel(UserAddress address) => new()
    {
        Id = address.Id,
        ContactName = address.ContactName,
        PhoneNumber = address.Phone,
        AddressText = FormatAddress(address),
        IsDefault = address.IsDefault
    };

    private static AccountFavoriteProductViewModel? ToFavoriteProductViewModel(Models.Entities.Wishlist wishlist)
    {
        var variant = wishlist.ProductVariant;
        var product = variant?.Product;
        if (variant is null || product is null)
        {
            return null;
        }

        var image = variant.ProductVariantImages
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .FirstOrDefault();
        var variantKey = GetVariantKey(variant);
        var name = BuildFavoriteProductName(product, variant);

        return new AccountFavoriteProductViewModel
        {
            ProductVariantKey = variantKey,
            Name = name,
            ImageUrl = NormalizeImageUrl(image?.ImagePath),
            ImageAlt = image?.AltText ?? name,
            PriceText = FormatCurrency(variant.Price),
            IsAvailable = variant.IsActive && product.IsActive && variant.Quantity > 0,
            AvailabilityText = variant.IsActive && product.IsActive && variant.Quantity > 0
                ? "Còn hàng"
                : "Tạm hết hàng",
            DetailUrl = $"/product/{Uri.EscapeDataString(product.Slug)}?variant={Uri.EscapeDataString(variantKey)}"
        };
    }

    private static AccountProfileOrderViewModel ToOrderViewModel(Order order)
    {
        var items = order.OrderItems
            .OrderBy(item => item.Id)
            .Select(ToOrderItemViewModel)
            .ToList();
        var firstItem = items.FirstOrDefault();
        var firstOrderItem = order.OrderItems.OrderBy(item => item.Id).FirstOrDefault();
        var itemCount = order.OrderItems.Sum(item => Math.Max(1, item.Quantity));
        var firstPrice = firstOrderItem is null ? 0m : firstOrderItem.UnitPrice;
        var orderCode = string.IsNullOrWhiteSpace(order.OrderCode) ? order.Id.ToString(ViCulture) : order.OrderCode.TrimStart('#');

        return new AccountProfileOrderViewModel
        {
            OrderCode = "#" + orderCode,
            OrderedDateText = order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy", ViCulture),
            Items = items,
            ProductName = firstItem?.ProductName ?? "Sản phẩm TechStore",
            ProductImageUrl = firstItem?.ProductImageUrl ?? FallbackImage,
            ProductImageAlt = firstItem?.ProductImageAlt ?? "Sản phẩm TechStore",
            ProductPriceText = FormatCurrency(firstPrice),
            OtherItemsText = itemCount > 1 ? $"Cùng {itemCount - 1} sản phẩm khác" : string.Empty,
            TotalText = FormatCurrency(order.TotalAmount),
            StatusText = GetStatusText(order.OrderStatus),
            StatusTone = GetStatusTone(order.OrderStatus),
            DetailUrl = BuildOrderDetailUrl(orderCode)
        };
    }

    private static AccountProfileOrderItemViewModel ToOrderItemViewModel(OrderItem item)
    {
        var variant = item.ProductVariant;
        var product = variant?.Product;
        var image = variant?.ProductVariantImages
            .OrderBy(image => image.Position)
            .FirstOrDefault();
        var quantity = Math.Max(1, item.Quantity);

        return new AccountProfileOrderItemViewModel
        {
            ProductName = product?.Name ?? "Sản phẩm TechStore",
            ProductImageUrl = NormalizeImageUrl(image?.ImagePath),
            ProductImageAlt = image?.AltText ?? product?.Name ?? "Sản phẩm TechStore",
            VariantText = BuildVariantText(variant),
            Quantity = quantity,
            LineTotalText = FormatCurrency(item.UnitPrice * quantity)
        };
    }

    private static string BuildVariantText(ProductVariant? variant)
    {
        if (variant is null)
        {
            return string.Empty;
        }

        var parts = variant.VariantAttributes
            .OrderBy(item => item.AttributeOption?.Attribute?.Id ?? item.AttributeOption?.AttributeId ?? 0)
            .ThenBy(item => item.AttributeOptionId)
            .Select(item => item.AttributeOption?.Label ?? item.AttributeOption?.Value)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(variant.ColorName)
            && !parts.Contains(variant.ColorName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            parts.Add(variant.ColorName.Trim());
        }

        return string.Join(" - ", parts);
    }

    private static string BuildFavoriteProductName(Product product, ProductVariant variant)
    {
        var variantText = BuildVariantText(variant);
        return string.IsNullOrWhiteSpace(variantText)
            ? product.Name
            : $"{product.Name} {variantText}";
    }

    private static string GetVariantKey(ProductVariant variant)
    {
        return string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Id.ToString(ViCulture)
            : variant.Code;
    }

    private static string GetStatusText(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đang xử lý",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Shipping => "Đang vận chuyển",
        OrderStatus.Completed => "Đã nhận hàng",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Returned => "Đã hủy",
        _ => "Đang xử lý"
    };

    private static string GetStatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "pending",
        OrderStatus.Cancelled or OrderStatus.Returned => "danger",
        _ => "success"
    };

    private static string GetGenderText(Gender gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        Gender.Other => "Khác",
        _ => "-"
    };

    private static string GetGenderValue(Gender gender) => gender switch
    {
        Gender.Male => "male",
        Gender.Female => "female",
        Gender.Other => "other",
        _ => "unknown"
    };

    private static string FormatAddress(UserAddress address)
    {
        if (!string.IsNullOrWhiteSpace(address.FormattedAddress))
        {
            return address.FormattedAddress.Trim();
        }

        var parts = new[]
        {
            address.DetailAddress,
            address.WardName,
            address.DistrictName,
            address.ProvinceName
        }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        var formatted = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(formatted) ? "-" : formatted;
    }

    private static string FormatCurrency(decimal value) =>
        value.ToString("N0", ViCulture) + "đ";

    private static string FormatVoucherDiscount(Voucher voucher)
    {
        if (voucher.DiscountType == DiscountType.Percentage)
        {
            var discount = $"Giảm {voucher.DiscountValue:N0}%";
            return voucher.MaxDiscountValue.HasValue && voucher.MaxDiscountValue.Value > 0
                ? $"{discount} tối đa {FormatCurrency(voucher.MaxDiscountValue.Value)}"
                : discount;
        }

        return $"Giảm {FormatCurrency(voucher.DiscountValue)}";
    }

    private static string BuildVoucherConditionText(Voucher voucher)
    {
        var parts = new List<string>();
        if (voucher.MinOrderValue > 0)
        {
            parts.Add($"Đơn từ {FormatCurrency(voucher.MinOrderValue)}");
        }
        else
        {
            parts.Add("Không yêu cầu giá trị đơn");
        }

        if (voucher.VoucherTargets.Count > 0)
        {
            parts.Add("Áp dụng cho sản phẩm phù hợp");
        }

        return string.Join(" · ", parts);
    }

    private static string BuildVoucherUsageText(
        int? remainingUses,
        int? perUserLimit,
        bool isAssignedToUser)
    {
        if (remainingUses.HasValue && perUserLimit.HasValue)
        {
            return remainingUses.Value > 0
                ? $"Còn {remainingUses.Value}/{perUserLimit.Value} lượt"
                : "Đã dùng hết lượt";
        }

        return isAssignedToUser ? "Dành riêng cho bạn" : "Còn hiệu lực";
    }

    private static string FormatDateInput(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    private static string FormatDate(DateTime value) =>
        value.ToString("dd/MM/yyyy", ViCulture);

    private static DateTime ToVietnamDateStartUtc(DateTime date) =>
        DateTime.SpecifyKind(date.Date.AddHours(-7), DateTimeKind.Utc);

    private static string BuildOrderDetailUrl(string orderCode) =>
        "/account/orders/" + Uri.EscapeDataString(orderCode.TrimStart('#'));

    private static string FormatDateTime(DateTime value) =>
        value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", ViCulture);

    private static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digits.Length < 5 ? phoneNumber.Trim() : $"{digits[..3]}*****{digits[^2..]}";
    }

    private static string NormalizeImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return FallbackImage;
        }

        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith('/'))
        {
            return imagePath;
        }

        return "/" + imagePath.TrimStart('/');
    }

    private sealed record OrderStatusCount(OrderStatus Status, int Count);
}
