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
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var emailLower = normalizedEmail.ToLowerInvariant();
        var user = string.IsNullOrWhiteSpace(emailLower)
            ? null
            : await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Email.ToLower() == emailLower,
                    cancellationToken);

        if (user is null)
        {
            return CreateFallbackPage(
                normalizedEmail,
                displayName,
                phoneNumber,
                activeTab);
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

        var orders = await dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.ProductVariantImages)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.VariantAttributes)
                        .ThenInclude(item => item.AttributeOption)
                            .ThenInclude(option => option!.Attribute)
            .Where(order => order.UserId == user.Id)
            .OrderByDescending(order => order.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var addresses = await dbContext.UserAddresses
            .AsNoTracking()
            .Where(address => address.UserId == user.Id && !address.IsDeleted)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAt ?? address.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderItems = orders.Select(ToOrderViewModel).ToList();
        var addressItems = addresses.Select(ToAddressViewModel).ToList();
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
            GenderText = GetGenderText(user.Gender),
            DefaultAddressText = defaultAddress?.AddressText ?? "-",
            PasswordUpdatedAtText = FormatDateTime(user.UpdatedAt ?? user.CreatedAt),
            OrderCountText = (summaryData?.Count ?? 0).ToString("N0", ViCulture),
            TotalSpentText = FormatCurrency(summaryData?.Total ?? 0m)
        };

        return new AccountProfilePageViewModel
        {
            ActiveTab = AccountProfileTabs.Normalize(activeTab),
            Summary = summary,
            RecentOrders = orderItems.Take(3).ToList(),
            Orders = orderItems,
            Addresses = addressItems
        };
    }

    private static AccountProfilePageViewModel CreateFallbackPage(
        string email,
        string? displayName,
        string? phoneNumber,
        string activeTab)
    {
        var resolvedPhone = phoneNumber?.Trim() ?? string.Empty;
        return new AccountProfilePageViewModel
        {
            ActiveTab = AccountProfileTabs.Normalize(activeTab),
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

    private static AccountProfileAddressViewModel ToAddressViewModel(UserAddress address) => new()
    {
        Id = address.Id,
        ContactName = address.ContactName,
        PhoneNumber = address.Phone,
        AddressText = FormatAddress(address),
        IsDefault = address.IsDefault
    };

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
}
