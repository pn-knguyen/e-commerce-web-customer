using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbHeaderAccountDataService(EcommerceDbContext dbContext)
    : IHeaderAccountDataService
{
    public async Task<HeaderAccountViewModel> GetAccountAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Email == normalizedEmail && item.IsActive)
            .Select(item => new
            {
                item.Email,
                item.FullName
            })
            .FirstOrDefaultAsync(cancellationToken);
        var fullName = user?.FullName ?? displayName;

        return new HeaderAccountViewModel
        {
            IsLoggedIn = true,
            DisplayName = ToButtonName(fullName),
            FullName = fullName,
            Email = user?.Email ?? normalizedEmail,
            Notifications = []
        };
    }

    private static string ToButtonName(string fullName)
    {
        var firstPart = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstPart) ? fullName : firstPart;
    }
}
