using System.Globalization;
using System.Text;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.ViewModels.Checkout;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Orders.Db;

public sealed class DbCheckoutPaymentMethodProvider(EcommerceDbContext dbContext)
    : ICheckoutPaymentMethodProvider
{
    public async Task<IReadOnlyList<CheckoutPaymentMethodViewModel>> GetActivePaymentMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        var methods = await dbContext.PaymentMethods
            .AsNoTracking()
            .Where(method => method.IsActive)
            .OrderBy(method => method.Id)
            .Select(method => new
            {
                method.Id,
                method.Name,
                method.Description
            })
            .ToListAsync(cancellationToken);

        return methods
            .Select(method =>
            {
                var iconKey = DetectIconKey(method.Name, method.Description);

                return new CheckoutPaymentMethodViewModel
                {
                    Id = method.Id,
                    Name = method.Name.Trim(),
                    Description = GetDescription(method.Description, iconKey),
                    IconKey = iconKey
                };
            })
            .ToList();
    }

    private static string GetDescription(string? description, string iconKey)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            return description.Trim();
        }

        return iconKey switch
        {
            "sepay" => "Quét QR chuyển khoản ngân hàng",
            "cod" => "Thanh toán khi nhận hàng",
            "banktransfer" => "Ngân hàng nội địa",
            "momo" => "Ví điện tử",
            "vnpay" => "Cổng thanh toán",
            "zalopay" => "Ví điện tử",
            _ => "Phương thức thanh toán"
        };
    }

    private static string DetectIconKey(string name, string? description)
    {
        var normalized = NormalizeText($"{name} {description}");

        if (normalized.Contains("sepay", StringComparison.Ordinal))
        {
            return "sepay";
        }

        if (normalized.Contains("momo", StringComparison.Ordinal))
        {
            return "momo";
        }

        if (normalized.Contains("vnpay", StringComparison.Ordinal))
        {
            return "vnpay";
        }

        if (normalized.Contains("zalopay", StringComparison.Ordinal))
        {
            return "zalopay";
        }

        if (normalized.Contains("cod", StringComparison.Ordinal)
            || normalized.Contains("nhanhang", StringComparison.Ordinal)
            || normalized.Contains("tienmat", StringComparison.Ordinal))
        {
            return "cod";
        }

        if (normalized.Contains("chuyenkhoan", StringComparison.Ordinal)
            || normalized.Contains("nganhang", StringComparison.Ordinal)
            || normalized.Contains("napas", StringComparison.Ordinal))
        {
            return "banktransfer";
        }

        return "generic";
    }

    private static string NormalizeText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }
}
