using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Account;

public interface IHeaderAccountProvider
{
    Task<HeaderAccountViewModel> GetAccountAsync(
        bool isLoggedIn,
        string? email,
        string? displayName,
        CancellationToken cancellationToken = default);
}
