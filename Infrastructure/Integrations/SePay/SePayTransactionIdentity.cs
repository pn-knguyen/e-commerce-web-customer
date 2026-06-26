using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using e_commerce_web_customer.Application.Contracts;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

internal static class SePayTransactionIdentity
{
    public static long Resolve(SePayWebhookNotification notification)
    {
        if (notification.Id > 0)
        {
            return notification.Id;
        }

        var fields = new[]
        {
            Normalize(notification.ReferenceCode),
            Normalize(notification.Gateway),
            Normalize(notification.TransactionDate),
            Normalize(notification.AccountNumber),
            Normalize(notification.SubAccount),
            Normalize(notification.Code),
            Normalize(notification.TransferType).ToLowerInvariant(),
            notification.TransferAmount.ToString(CultureInfo.InvariantCulture),
            notification.Accumulated.ToString(CultureInfo.InvariantCulture),
            Normalize(notification.Content),
            Normalize(notification.Description)
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", fields)));
        var value = BitConverter.ToInt64(hash, 0) & long.MaxValue;
        return value == 0 ? 1 : value;
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
