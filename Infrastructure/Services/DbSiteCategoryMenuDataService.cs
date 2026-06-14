using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbSiteCategoryMenuDataService(EcommerceDbContext dbContext)
    : ISiteCategoryMenuDataService
{
    public async Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var roots = categories.Where(item => item.ParentId is null).ToList();

        if (roots.Count == 0)
        {
            roots = categories;
        }

        return new SiteCategoryMenuViewModel
        {
            Items = roots.Select(category =>
            {
                var children = categories
                    .Where(item => item.ParentId == category.Id)
                    .ToList();

                return new SiteCategoryMenuItemViewModel
                {
                    Id = category.Slug,
                    Url = $"/catalog/{category.Slug}",
                    Label = category.Name,
                    Icon = ResolveIcon(category.Slug),
                    Groups = children.Count == 0
                        ? []
                        :
                        [
                            new SiteCategoryMenuGroupViewModel
                            {
                                Title = category.Name,
                                Links = children.Select(child =>
                                    new SiteCategoryMenuLinkViewModel
                                    {
                                        Label = child.Name,
                                        Url = $"/catalog/{child.Slug}",
                                        ImageUrl = child.ImagePath,
                                        ImageAlt = child.Name
                                    }).ToList()
                            }
                        ]
                };
            }).ToList()
        };
    }

    private static string ResolveIcon(string slug)
    {
        var value = slug.ToLowerInvariant();

        if (value.Contains("phone") || value.Contains("dien-thoai")) return "phone";
        if (value.Contains("laptop") || value.Contains("computer")) return "laptop";
        if (value.Contains("audio") || value.Contains("am-thanh")) return "headphones";
        if (value.Contains("watch") || value.Contains("dong-ho")) return "watch";
        if (value.Contains("accessor") || value.Contains("phu-kien")) return "accessories";
        if (value.Contains("tv") || value.Contains("tivi")) return "tv";

        return "category";
    }
}
