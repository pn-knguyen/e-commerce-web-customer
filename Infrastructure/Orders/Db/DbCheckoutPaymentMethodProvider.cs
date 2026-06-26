using e_commerce_web_customer.Application.Constants;
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
            .Select(method => new CheckoutPaymentMethodViewModel
            {
                Id = method.Id,
                Name = method.Name.Trim(),
                Description = method.Description?.Trim() ?? string.Empty,
                IconKey = GetIconKey(method.Name)
            })
            .ToList();
    }

    private static string GetIconKey(string name)
    {
        var lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("sepay")) return "banktransfer";
        if (lowerName.Contains("momo")) return "momo";
        if (lowerName.Contains("vnpay")) return "vnpay";
        if (lowerName.Contains("zalopay")) return "zalopay";
        if (lowerName.Contains("cod") || lowerName.Contains("nhận hàng")) return "cod";
        if (lowerName.Contains("chuyển khoản") || lowerName.Contains("ngân hàng")) return "banktransfer";
        return "generic";
    }
}
