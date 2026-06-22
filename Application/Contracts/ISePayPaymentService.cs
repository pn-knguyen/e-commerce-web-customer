namespace e_commerce_web_customer.Application.Contracts;

public interface ISePayPaymentService
{
    Task<SePayPaymentDetails?> GetPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task<SePayOrderPaymentStatus?> GetStatusAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default);
}

public sealed record SePayPaymentDetails(
    string OrderCode,
    decimal Amount,
    string PaymentStatus,
    string BankCode,
    string BankName,
    string AccountNumber,
    string AccountName,
    string QrUrl,
    DateTimeOffset ExpiresAt,
    bool IsTestMode);

public sealed record SePayOrderPaymentStatus(string Status);
