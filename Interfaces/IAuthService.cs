using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Interfaces;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password, bool rememberMe);

    Task<bool> RegisterAsync(RegisterViewModel model);

    void Logout();

    Task<bool> UserExistsAsync(string email);
}
