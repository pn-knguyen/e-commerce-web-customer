using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.ViewModels.Catalog;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbCategoryPageDataService(EcommerceDbContext dbContext)
    : ICategoryPageDataService
{
    public async Task<CategoryPageViewModel?> CreateCategoryPageAsync(
        CategoryPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);
        var category = ResolveCategory(categories, request.Slug);

        if (category is null)
        {
            return null;
        }

        var categoryIds = categories
            .Where(item => item.Id == category.Id || item.ParentId == category.Id)
            .Select(item => item.Id)
            .ToList();
        var productQuery = dbContext.Products
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.Category)
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages)
            .Where(item =>
                item.IsActive &&
                categoryIds.Contains(item.CategoryId) &&
                item.ProductVariants.Any(variant => variant.IsActive));

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            var brand = request.Brand.Trim();
            productQuery = productQuery.Where(item =>
                item.Brand.Slug == brand || item.Brand.Name == brand);
        }

        var products = await productQuery
            .OrderByDescending(item => item.IsFeatured)
            .ThenByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var readModels = products
            .Select(item =>
            {
                var variant = item.ProductVariants
                    .Where(variant => variant.IsActive)
                    .OrderByDescending(variant => variant.IsDefault)
                    .ThenBy(variant => variant.Id)
                    .First();

                return DbProductMapper.ToReadModel(item, variant);
            })
            .ToList();

        readModels = NormalizeSort(request.Sort) switch
        {
            "price-desc" => readModels.OrderByDescending(item => item.CurrentPrice).ToList(),
            "price-asc" => readModels.OrderBy(item => item.CurrentPrice).ToList(),
            _ => readModels
        };

        var cards = readModels.Select(ProductViewModelMapper.ToProductCard).ToList();
        var childCategories = categories
            .Where(item => item.ParentId == category.Id)
            .ToList();
        var isSectioned = IsAudioCategory(category.Slug, category.Name);

        return new CategoryPageViewModel
        {
            Slug = category.Slug,
            Title = category.Name,
            MetaDescription = category.Description ?? $"Danh muc {category.Name} tai TechStore.",
            LayoutMode = isSectioned
                ? CategoryPageLayoutMode.Sectioned
                : CategoryPageLayoutMode.FilterListing,
            Breadcrumbs =
            [
                new() { Label = "Trang chu", Url = "/" },
                new()
                {
                    Label = category.Name,
                    Url = $"/catalog/{category.Slug}",
                    IsCurrent = true
                }
            ],
            PromotionBanners = [],
            Brands = products
                .Select(item => item.Brand)
                .DistinctBy(item => item.Id)
                .OrderBy(item => item.Name)
                .Select(item => new CategoryBrandViewModel
                {
                    Id = item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Label = item.Name,
                    Url = $"/catalog/{category.Slug}?brand={Uri.EscapeDataString(item.Slug)}"
                })
                .ToList(),
            QuickLinks = childCategories.Select(item => new CategoryQuickLinkViewModel
            {
                Label = item.Name,
                Url = $"/catalog/{item.Slug}",
                ImageUrl = item.ImagePath ?? "/images/logo-techstore-icon.svg",
                ImageAlt = item.Name
            }).ToList(),
            HotSale = new CategoryHotSaleViewModel
            {
                Title = "San pham noi bat",
                Products = cards.Take(8).ToList()
            },
            Filter = CreateFilter(category.Slug, request),
            Products = cards,
            InitialProductCount = 20,
            SectionTabs = [],
            ProductSections = [],
            SeoContent = new CategorySeoContentViewModel
            {
                Title = $"Thong tin danh muc {category.Name}",
                Paragraphs = string.IsNullOrWhiteSpace(category.Description)
                    ? []
                    : [category.Description]
            },
            QuestionAnswer = new QuestionAnswerSectionViewModel
            {
                Title = $"Hoi dap ve {category.Name}",
                FormTitle = "Ban can tu van?",
                Description = "Hay gui cau hoi de TechStore ho tro.",
                Placeholder = "Viet cau hoi cua ban tai day",
                SubmitLabel = "Gui cau hoi",
                Threads = []
            }
        };
    }

    private static Models.Category? ResolveCategory(
        IReadOnlyList<Models.Category> categories,
        string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var exactMatch = categories.FirstOrDefault(item =>
            item.Slug.Equals(normalizedSlug, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return exactMatch;
        }

        var aliases = normalizedSlug switch
        {
            "phone" or "mobile" => new[] { "phone", "dien-thoai", "mobile" },
            "audio" or "am-thanh" => new[] { "audio", "am-thanh", "tai-nghe", "loa" },
            _ => [normalizedSlug]
        };

        return categories.FirstOrDefault(item =>
        {
            var searchable = SearchTextNormalizer.Normalize($"{item.Slug} {item.Name}");
            return aliases.Any(alias =>
                searchable.Contains(
                    SearchTextNormalizer.Normalize(alias),
                    StringComparison.Ordinal));
        });
    }

    private static CategoryFilterViewModel CreateFilter(
        string slug,
        CategoryPageRequest request)
    {
        var activeSort = NormalizeSort(request.Sort);

        return new CategoryFilterViewModel
        {
            Title = "Chon theo tieu chi",
            PrimaryItems = [],
            SecondaryItems = [],
            SortOptions =
            [
                SortOption("Lien quan", null, slug, activeSort),
                SortOption("Gia cao", "price-desc", slug, activeSort),
                SortOption("Gia thap", "price-asc", slug, activeSort)
            ]
        };
    }

    private static CategorySortOptionViewModel SortOption(
        string label,
        string? sort,
        string slug,
        string activeSort)
    {
        var value = sort ?? "relevance";

        return new CategorySortOptionViewModel
        {
            Label = label,
            Url = sort is null
                ? $"/catalog/{slug}"
                : $"/catalog/{slug}?sort={sort}",
            IsActive = value == activeSort
        };
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "price-desc" => "price-desc",
            "price-asc" => "price-asc",
            _ => "relevance"
        };
    }

    private static bool IsAudioCategory(string slug, string name)
    {
        var value = SearchTextNormalizer.Normalize($"{slug} {name}");
        return value.Contains("audio", StringComparison.Ordinal) ||
               value.Contains("am thanh", StringComparison.Ordinal);
    }
}
