using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Infrastructure.Account.Mock;

public sealed class MockAuthService(IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private sealed record MockUser(string Email, string Password, string FullName, string? PhoneNumber);

    private static readonly List<MockUser> Users =
    [
        new("demo@techstore.vn", "Password123", "Nguyễn Văn Demo", "0987654321")
    ];

    private static readonly object Lock = new();

    public Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        var normalizedEmail = NormalizeEmail(email);
        bool authenticated;

        lock (Lock)
        {
            authenticated = Users.Any(user =>
                user.Email == normalizedEmail && user.Password == password);
        }

        if (authenticated)
        {
            var session = GetSession();
            session.SetString(SessionKeys.IsLoggedIn, "true");
            session.SetString(SessionKeys.UserEmail, normalizedEmail);
            session.Remove(SessionKeys.UserId);
        }

        return Task.FromResult(authenticated);
    }

    public Task<bool> RegisterAsync(RegisterViewModel model)
    {
        var normalizedEmail = NormalizeEmail(model.Email);

        lock (Lock)
        {
            if (Users.Any(user => user.Email == normalizedEmail))
            {
                return Task.FromResult(false);
            }

            Users.Add(new MockUser(
                normalizedEmail,
                model.Password,
                model.FullName.Trim(),
                model.PhoneNumber?.Trim()));
        }

        return Task.FromResult(true);
    }

    public void Logout()
    {
        GetSession().Clear();
    }

    public Task<bool> UserExistsAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        lock (Lock)
        {
            return Task.FromResult(Users.Any(user => user.Email == normalizedEmail));
        }
    }

    private ISession GetSession()
    {
        return httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HTTP session is not available.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
