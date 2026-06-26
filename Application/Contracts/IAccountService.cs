using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Application.Contracts;

/// <summary>
/// Defines the authentication and account operations.
/// This interface decouples the controller from the data access/identity layer,
/// allowing easy switching between mock implementation and a real database (e.g. EF Core, Identity).
/// </summary>
public interface IAccountService
{


    /// <summary>
    /// Gets the lightweight profile needed by shared UI after a successful login.
    /// </summary>
    Task<AccountProfile?> GetProfileAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<bool> RegisterAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user email already exists in the system.
    /// </summary>
    Task<bool> UserExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the email associated with a phone number. 
    /// </summary>
    Task<string?> FindEmailByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);
}

public sealed record AccountProfile(string Email, string DisplayName, string? PhoneNumber = null);
