using e_commerce_web_customer.Infrastructure.Integrations.SePay;

namespace e_commerce_web_customer.Application.Contracts;

public interface ISePayWebhookService
{
    Task<SePayWebhookReceiveResult> ReceiveAsync(
        SePayWebhookPayload payload,
        string rawPayload,
        CancellationToken cancellationToken = default);
}

public sealed record SePayWebhookReceiveResult(bool IsDuplicate);
