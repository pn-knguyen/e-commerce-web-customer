using e_commerce_web_customer.Application.CustomerMessages;

namespace e_commerce_web_customer.Infrastructure.CustomerMessages;

public sealed class MockCustomerMessageCustomerService : ICustomerMessageCustomerService
{
    public Task<long?> ResolveActiveCustomerIdAsync(
        string? email,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<long?>(null);

    public Task<CustomerChatBootstrap> GetBootstrapAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CustomerChatBootstrap(
            false,
            null,
            displayName?.Trim() ?? email?.Trim() ?? string.Empty,
            null,
            null,
            []));
    }
}
