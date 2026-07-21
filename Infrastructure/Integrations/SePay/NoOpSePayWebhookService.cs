using e_commerce_web_customer.Application.Contracts;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class NoOpSePayWebhookService : ISePayWebhookService
{
    public Task<SePayWebhookReceiveResult> ReceiveAsync(
        SePayWebhookNotification notification,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        _ = notification;
        _ = rawPayload;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new SePayWebhookReceiveResult(false));
    }
}
