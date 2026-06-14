using System.Text;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbAccountService(EcommerceDbContext dbContext)
    : IAccountService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<bool> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        _ = rememberMe;
        var normalizedEmail = NormalizeEmail(email);
        var user = await dbContext.Users.FirstOrDefaultAsync(
            item => item.Email == normalizedEmail && item.IsActive,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<AccountProfile?> GetProfileAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        return await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Email == normalizedEmail && item.IsActive)
            .Select(item => new AccountProfile(item.Email, item.FullName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RegisterAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(model.Email);
        if (await dbContext.Users.AnyAsync(
                item => item.Email == normalizedEmail,
                cancellationToken))
        {
            return false;
        }

        var user = new User
        {
            Username = await CreateUniqueUsernameAsync(
                normalizedEmail,
                cancellationToken),
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim(),
            Gender = "Other",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public Task<bool> UserExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        return dbContext.Users.AnyAsync(
            item => item.Email == normalizedEmail,
            cancellationToken);
    }

    private async Task<string> CreateUniqueUsernameAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var baseUsername = SanitizeUsername(email.Split('@')[0]);
        var username = baseUsername;
        var suffix = 1;

        while (await dbContext.Users.AnyAsync(
                   item => item.Username == username,
                   cancellationToken))
        {
            username = $"{baseUsername}{suffix}";
            suffix++;
        }

        return username;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string SanitizeUsername(string value)
    {
        var builder = new StringBuilder();

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        var username = builder.ToString();
        return string.IsNullOrWhiteSpace(username)
            ? "customer"
            : username[..Math.Min(username.Length, 90)];
    }
}
