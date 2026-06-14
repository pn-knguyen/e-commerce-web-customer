using System.Text;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Services;

public class AuthService : IAuthService
{
    private readonly EcommerceDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        EcommerceDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail && item.IsActive);

        if (user is null)
        {
            return false;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return false;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        var session = GetSession();
        session.SetString(SessionKeys.IsLoggedIn, "true");
        session.SetString(SessionKeys.UserId, user.Id.ToString());
        session.SetString(SessionKeys.UserEmail, user.Email);

        return true;
    }

    public async Task<bool> RegisterAsync(RegisterViewModel model)
    {
        var normalizedEmail = NormalizeEmail(model.Email);

        if (await _dbContext.Users.AnyAsync(user => user.Email == normalizedEmail))
        {
            return false;
        }

        var user = new User
        {
            Username = await CreateUniqueUsernameAsync(normalizedEmail),
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
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public void Logout()
    {
        GetSession().Clear();
    }

    public Task<bool> UserExistsAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return _dbContext.Users.AnyAsync(user => user.Email == normalizedEmail);
    }

    private ISession GetSession()
    {
        return _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("HTTP session is not available.");
    }

    private async Task<string> CreateUniqueUsernameAsync(string email)
    {
        var baseUsername = SanitizeUsername(email.Split('@')[0]);
        var username = baseUsername;
        var suffix = 1;

        while (await _dbContext.Users.AnyAsync(user => user.Username == username))
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
        return string.IsNullOrWhiteSpace(username) ? "customer" : username[..Math.Min(username.Length, 90)];
    }
}
