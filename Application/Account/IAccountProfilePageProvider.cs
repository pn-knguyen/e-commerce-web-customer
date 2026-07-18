using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Application.Account;

public interface IAccountProfilePageProvider
{
    Task<AccountProfilePageViewModel> GetProfilePageAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string activeTab,
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default);

    Task<AccountOrderHistoryViewModel> GetOrderHistoryAsync(
        string? email,
        string? orderStatus,
        string? fromDate,
        string? toDate,
        CancellationToken cancellationToken = default);
}
