using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Account.Db;

public sealed class DbAccountService(
    EcommerceDbContext dbContext) : IAccountService
{


    public async Task<AccountProfile?> GetProfileAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        var user = await FindUserByEmailAsync(normalizedEmail, asTracking: false, cancellationToken);
        return user is null
            ? null
            : new AccountProfile(user.Email, ResolveDisplayName(user), user.Phone);
    }

    public async Task<bool> RegisterAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(model.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        var exists = await UserExistsAsync(normalizedEmail, cancellationToken);
        if (exists)
        {
            return false;
        }

        var user = new User
        {
            Username = await GenerateUniqueUsernameAsync(normalizedEmail, cancellationToken),
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = model.Password;

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

    public async Task<bool> UserExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        var emailLower = normalizedEmail.ToLowerInvariant();
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email.ToLower() == emailLower, cancellationToken);
    }

    public async Task<string?> FindEmailByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        var phone1 = phoneNumber.Trim();
        var phone2 = phone1;

        if (phone1.StartsWith("+84"))
        {
            phone2 = "0" + phone1[3..];
        }
        else if (phone1.StartsWith("0"))
        {
            phone2 = "+84" + phone1[1..];
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone1 || u.Phone == phone2, cancellationToken);

        return user?.Email;
    }

    private Task<User?> FindUserByEmailAsync(
        string normalizedEmail,
        bool asTracking,
        CancellationToken cancellationToken)
    {
        var emailLower = normalizedEmail.ToLowerInvariant();
        var query = asTracking
            ? dbContext.Users
            : dbContext.Users.AsNoTracking();

        return query.FirstOrDefaultAsync(
            user => user.Email.ToLower() == emailLower,
            cancellationToken);
    }

    private async Task<string> GenerateUniqueUsernameAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var baseUsername = CreateUsernameBase(normalizedEmail);
        var username = baseUsername;
        var suffix = 2;

        while (await UsernameExistsAsync(username, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, 100 - suffixText.Length);
            username = $"{baseUsername[..Math.Min(baseUsername.Length, maxBaseLength)]}{suffixText}";
            suffix++;
        }

        return username;
    }

    private async Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var usernameLower = username.ToLowerInvariant();
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Username.ToLower() == usernameLower, cancellationToken);
    }

    private static string CreateUsernameBase(string normalizedEmail)
    {
        var localPart = normalizedEmail.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var rawUsername = string.IsNullOrWhiteSpace(localPart) ? "user" : localPart;
        var safeCharacters = rawUsername
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-'
                ? character
                : '-')
            .ToArray();

        var username = new string(safeCharacters).Trim('-', '.', '_');
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "user";
        }

        return username.Length <= 100 ? username : username[..100];
    }

    private static string ResolveDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName;
        }

        return string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;
    }

    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim();
    }
}
