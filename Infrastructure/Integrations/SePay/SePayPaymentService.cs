using System.Globalization;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayPaymentService(
    EcommerceDbContext dbContext,
    IOptions<SePayPaymentOptions> options) : ISePayPaymentService
{
    private readonly SePayPaymentOptions _options = options.Value;

    public async Task<SePayPaymentDetails?> GetPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var order = await FindOwnedOrder(orderCode, userEmail)
            .Select(item => new
            {
                item.OrderCode,
                item.TotalAmount,
                item.PaymentStatus,
                item.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null) return null;

        var amount = SePayVndAmount.Normalize(order.TotalAmount);
        var qrUrl = BuildQrUrl(order.OrderCode, SePayVndAmount.ToWholeUnits(amount));
        var timeout = Math.Clamp(_options.PaymentTimeoutMinutes, 1, 24 * 60);

        return new SePayPaymentDetails(
            order.OrderCode,
            amount,
            order.PaymentStatus.ToString().ToLowerInvariant(),
            _options.BankCode,
            _options.BankName,
            _options.AccountNumber,
            _options.AccountName,
            qrUrl,
            new DateTimeOffset(order.CreatedAt, TimeSpan.Zero).AddMinutes(timeout),
            _options.IsTestMode);
    }

    public async Task<SePayOrderPaymentStatus?> GetStatusAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var order = await FindOwnedOrder(orderCode, userEmail)
            .Select(item => new { item.PaymentStatus })
            .FirstOrDefaultAsync(cancellationToken);

        return order is null
            ? null
            : new(order.PaymentStatus.ToString().ToLowerInvariant());
    }

    private IQueryable<Models.Entities.Order> FindOwnedOrder(string orderCode, string userEmail)
    {
        var normalizedCode = orderCode.Trim();
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        return dbContext.Orders
            .AsNoTracking()
            .Where(item =>
                item.OrderCode == normalizedCode
                && item.PaymentMethod != null
                && item.PaymentMethod.Name.ToLower().Contains("sepay")
                && item.User != null
                && item.User.Email.ToLower() == normalizedEmail);
    }

    private string BuildQrUrl(string orderCode, long amount)
    {
        if (string.IsNullOrWhiteSpace(_options.AccountNumber)
            || string.IsNullOrWhiteSpace(_options.BankCode))
        {
            return string.Empty;
        }

        return "https://qr.sepay.vn/img"
            + $"?acc={Uri.EscapeDataString(_options.AccountNumber)}"
            + $"&bank={Uri.EscapeDataString(_options.BankCode)}"
            + $"&amount={amount.ToString(CultureInfo.InvariantCulture)}"
            + $"&des={Uri.EscapeDataString(orderCode)}";
    }
}
