namespace e_commerce_web_customer.Application.CustomerMessages;

public interface ICustomerMessageCustomerService
{
    Task<long?> ResolveActiveCustomerIdAsync(
        string? email,
        CancellationToken cancellationToken = default);

    Task<CustomerChatBootstrap> GetBootstrapAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string? channel,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerChatBootstrap(
    bool IsLoggedIn,
    long? UserId,
    string DisplayName,
    long? ConversationId,
    string? Subject,
    IReadOnlyList<CustomerChatMessage> Messages);

public sealed record CustomerChatMessage(
    long Id,
    long ConversationId,
    string Sender,
    string SenderLabel,
    string SenderName,
    string Body,
    string? AiProvider,
    string? AiModel,
    string? AiMetadataJson,
    string CreatedAtIso,
    string CreatedAtText);
