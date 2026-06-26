namespace e_commerce_web_customer.Application.Contracts;

public interface ISePayWebhookService
{
    Task<SePayWebhookReceiveResult> ReceiveAsync(
        SePayWebhookNotification notification,
        string rawPayload,
        CancellationToken cancellationToken = default);
}

public sealed record SePayWebhookReceiveResult(bool IsDuplicate);
