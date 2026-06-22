using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookService(
    EcommerceDbContext dbContext,
    IOptions<SePayWebhookOptions> options,
    IOptions<SePayPaymentOptions> paymentOptions) : ISePayWebhookService
{
    private static readonly Regex OrderCodePattern = new(
        @"ORD-\d{8}-[A-Z0-9]{8}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly SePayWebhookOptions _options = options.Value;
    private readonly SePayPaymentOptions _paymentOptions = paymentOptions.Value;

    public async Task<SePayWebhookReceiveResult> ReceiveAsync(
        SePayWebhookPayload payload,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.SePayWebhookEvents.AnyAsync(
                item => item.SePayTransactionId == payload.Id,
                cancellationToken))
        {
            return new(true);
        }

        var webhookEvent = new SePayWebhookEvent
        {
            SePayTransactionId = payload.Id,
            Gateway = payload.Gateway,
            TransactionDate = payload.TransactionDate,
            AccountNumber = payload.AccountNumber,
            SubAccount = payload.SubAccount,
            Code = payload.Code,
            Content = payload.Content,
            TransferType = payload.TransferType,
            Description = payload.Description,
            TransferAmount = payload.TransferAmount,
            Accumulated = payload.Accumulated,
            ReferenceCode = payload.ReferenceCode,
            RawPayload = rawPayload,
            IsTestMode = _options.IsTestMode,
            ProcessingStatus = "Received",
            ReceivedAt = DateTime.UtcNow
        };

        await ApplyToOrderAsync(webhookEvent, payload, cancellationToken);
        dbContext.SePayWebhookEvents.Add(webhookEvent);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(false);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            dbContext.ChangeTracker.Clear();
            return new(true);
        }
    }

    private async Task ApplyToOrderAsync(
        SePayWebhookEvent webhookEvent,
        SePayWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        webhookEvent.ProcessedAt = DateTime.UtcNow;

        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            SetResult(webhookEvent, "Ignored", "Giao dịch không phải tiền vào.");
            return;
        }

        var orderCode = ExtractOrderCode(payload.Code) ?? ExtractOrderCode(payload.Content);
        if (orderCode is null)
        {
            SetResult(webhookEvent, "Unmatched", "Không tìm thấy mã đơn hàng trong giao dịch.");
            return;
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(
            item => item.OrderCode == orderCode
                && item.PaymentMethodId == _paymentOptions.PaymentMethodId,
            cancellationToken);

        if (order is null)
        {
            SetResult(webhookEvent, "Unmatched", $"Không tìm thấy đơn SePay {orderCode}.");
            return;
        }

        webhookEvent.MatchedOrderId = order.Id;

        if (!string.IsNullOrWhiteSpace(_paymentOptions.AccountNumber)
            && !string.Equals(
                payload.AccountNumber.Trim(),
                _paymentOptions.AccountNumber.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            SetResult(webhookEvent, "Rejected", "Số tài khoản nhận không khớp cấu hình.");
            return;
        }

        if (payload.TransferAmount != order.TotalAmount)
        {
            SetResult(
                webhookEvent,
                "Rejected",
                $"Số tiền nhận {payload.TransferAmount:0.##} không khớp đơn hàng {order.TotalAmount:0.##}.");
            return;
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            SetResult(webhookEvent, "AlreadyPaid", "Đơn hàng đã được thanh toán trước đó.");
            return;
        }

        if (order.PaymentStatus != PaymentStatus.Unpaid)
        {
            SetResult(webhookEvent, "Rejected", $"Trạng thái đơn hiện tại: {order.PaymentStatus}.");
            return;
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        SetResult(webhookEvent, "Paid", "Đã xác nhận thanh toán đơn hàng.");
    }

    private static string? ExtractOrderCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = OrderCodePattern.Match(value);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static void SetResult(
        SePayWebhookEvent webhookEvent,
        string status,
        string message)
    {
        webhookEvent.ProcessingStatus = status;
        webhookEvent.ProcessingMessage = message;
    }
}
