using System.Security.Cryptography;
using System.Text;
using e_commerce_web_customer.Data;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Caching;

internal static class CategoryCacheTokenBuilder
{
    public static async Task<string> CreateAsync(
        EcommerceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .Select(category => new CategoryCacheRow(
                category.Id,
                category.ParentId,
                category.Name,
                category.Slug,
                category.ImagePath,
                category.Position,
                category.IsActive,
                category.UpdatedAt))
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder(categories.Count * 96);
        foreach (var category in categories)
        {
            builder
                .Append(category.Id)
                .Append('|')
                .Append(category.ParentId)
                .Append('|')
                .Append(category.Name)
                .Append('|')
                .Append(category.Slug)
                .Append('|')
                .Append(category.ImagePath)
                .Append('|')
                .Append(category.Position)
                .Append('|')
                .Append(category.IsActive ? '1' : '0')
                .Append('|')
                .Append(category.UpdatedAt?.Ticks ?? 0)
                .Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private sealed record CategoryCacheRow(
        long Id,
        long? ParentId,
        string Name,
        string Slug,
        string? ImagePath,
        int Position,
        bool IsActive,
        DateTime? UpdatedAt);
}
