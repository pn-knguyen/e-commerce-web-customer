using e_commerce_web_customer.Application.Contracts;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class NoOpSePayPaymentService : ISePayPaymentService
{
    public Task<SePayPaymentDetails?> GetPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        _ = orderCode;
        _ = userEmail;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<SePayPaymentDetails?>(null);
    }

    public Task<SePayOrderPaymentStatus?> GetStatusAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        _ = orderCode;
        _ = userEmail;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<SePayOrderPaymentStatus?>(null);
    }
}
