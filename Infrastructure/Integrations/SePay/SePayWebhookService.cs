using System.Text.RegularExpressions;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookService(
    EcommerceDbContext dbContext,
    IOptions<SePayWebhookOptions> options,
    IOptions<SePayPaymentOptions> paymentOptions,
    IOrderService orderService) : ISePayWebhookService
{
    private static readonly Regex OrderCodePattern = new(
        @"ORD-?\d{8}-?[A-Z0-9]{8}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly SePayWebhookOptions _options = options.Value;
    private readonly SePayPaymentOptions _paymentOptions = paymentOptions.Value;

    public async Task<SePayWebhookReceiveResult> ReceiveAsync(
        SePayWebhookNotification notification,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        var transactionId = SePayTransactionIdentity.Resolve(notification);
        if (await dbContext.SePayWebhookEvents.AnyAsync(
                item => item.SePayTransactionId == transactionId,
                cancellationToken))
        {
            return new(true);
        }

        var webhookEvent = new SePayWebhookEvent
        {
            SePayTransactionId = transactionId,
            Gateway = notification.Gateway,
            TransactionDate = notification.TransactionDate,
            AccountNumber = notification.AccountNumber,
            SubAccount = notification.SubAccount,
            Code = notification.Code,
            Content = notification.Content,
            TransferType = notification.TransferType,
            Description = notification.Description,
            TransferAmount = notification.TransferAmount,
            Accumulated = notification.Accumulated,
            ReferenceCode = notification.ReferenceCode,
            RawPayload = rawPayload,
            IsTestMode = _options.IsTestMode,
            ProcessingStatus = "Received",
            ReceivedAt = DateTime.UtcNow
        };

        await ApplyToOrderAsync(webhookEvent, notification, cancellationToken);
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
        SePayWebhookNotification notification,
        CancellationToken cancellationToken)
    {
        webhookEvent.ProcessedAt = DateTime.UtcNow;

        if (!string.Equals(notification.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            SetResult(webhookEvent, "Ignored", "Transaction is not inbound.");
            return;
        }

        var orderCode = ExtractOrderCode(notification.Code)
            ?? ExtractOrderCode(notification.Content)
            ?? ExtractOrderCode(notification.Description);
        if (orderCode is null)
        {
            SetResult(webhookEvent, "Unmatched", "No order code found in transaction.");
            return;
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(
            item => item.OrderCode == orderCode
                && item.PaymentMethod != null
                && item.PaymentMethod.Name.ToLower().Contains("sepay"),
            cancellationToken);

        if (order is null)
        {
            SetResult(webhookEvent, "Unmatched", $"SePay order {orderCode} was not found.");
            return;
        }

        webhookEvent.MatchedOrderId = order.Id;

        if (string.IsNullOrWhiteSpace(_paymentOptions.AccountNumber))
        {
            SetResult(webhookEvent, "Rejected", "Receiving account number is not configured.");
            return;
        }

        if (!string.Equals(
                notification.AccountNumber.Trim(),
                _paymentOptions.AccountNumber.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            SetResult(webhookEvent, "Rejected", "Receiving account number does not match configuration.");
            return;
        }

        var expectedAmount = SePayVndAmount.Normalize(order.TotalAmount);
        if (notification.TransferAmount != expectedAmount)
        {
            SetResult(
                webhookEvent,
                "Rejected",
                $"Received amount {notification.TransferAmount:0.##} does not match payable amount {expectedAmount:0}.");
            return;
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            SetResult(webhookEvent, "AlreadyPaid", "Order was already paid.");
            return;
        }

        if (order.PaymentStatus != PaymentStatus.Unpaid)
        {
            SetResult(webhookEvent, "Rejected", $"Current order payment status is {order.PaymentStatus}.");
            return;
        }

        await orderService.ConfirmOnlinePaymentAsync(orderCode, cancellationToken);
        SetResult(webhookEvent, "Paid", "Order payment was confirmed.");
    }

    private static string? ExtractOrderCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = OrderCodePattern.Match(value);
        if (!match.Success) return null;

        var raw = match.Value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return raw.Length == 19
            ? $"{raw[..3]}-{raw.Substring(3, 8)}-{raw.Substring(11, 8)}"
            : match.Value.ToUpperInvariant();
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
