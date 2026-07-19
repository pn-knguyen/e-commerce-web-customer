using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Infrastructure.Home.Content;
using e_commerce_web_customer.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Home.Db;

public sealed class DbHomePageDataService(
    IDbContextFactory<EcommerceDbContext> dbContextFactory,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate,
    ILogger<DbHomePageDataService> logger) : IHomePageDataService
{
    private const int MaxVariantsPerPanel = 10;
    private const string CategoryFallbackImage = "/images/logo-techstore-icon.svg";

    private static readonly string[] PhoneCategorySlugs =
    [
        "phone",
        "dien-thoai",
        "smartphone",
        "dien-thoai-di-dong"
    ];

    private static readonly string[] TabletCategorySlugs =
    [
        "tablet",
        "may-tinh-bang"
    ];

    private static readonly string[] TvCategorySlugs =
    [
        "tv",
        "tivi"
    ];

    private static readonly CategorySectionDefinition[] ComputerSectionDefinitions =
    [
        new("laptops", "Laptop", "laptop", true, true),
        new("desktop-pcs", "PC", "pc", ShowBrandFilter: true),
        new("monitors", "Màn hình", "man-hinh", ShowBrandFilter: true),
        new("computer-accessories", "Phụ kiện máy tính", "phu-kien-may-tinh", ShowBrandFilter: true)
    ];

    private static readonly CategorySectionDefinition[] AudioWearableSectionDefinitions =
    [
        new("watches", "Đồng hồ", "dong-ho", IsActive: true, ShowBrandFilter: true),
        new("audio", "Âm thanh", "am-thanh", ShowBrandFilter: true)
    ];

    private static readonly CategorySectionDefinition[] TvSectionDefinitions =
    [
        new("tv", "TIVI", "tv", IsActive: true, ShowBrandFilter: true)
    ];

    public async Task<HomeIndexViewModel> CreateHomePageAsync(
        SiteCategoryMenuViewModel categoryMenu,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var categoryCacheToken = await GetCategoryCacheTokenAsync(cancellationToken);

        return await cache.GetOrCreateExclusiveAsync(
            $"home-page-v5:{categoryCacheToken}",
            () => CreateHomePageUncachedAsync(categoryMenu, CancellationToken.None),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
    }

    private async Task<HomeIndexViewModel> CreateHomePageUncachedAsync(
        SiteCategoryMenuViewModel categoryMenu,
        CancellationToken cancellationToken)
    {
        var categories = await GetActiveCategoriesAsync(cancellationToken);
        var phoneTabletSectionTask = CreatePhoneTabletSectionAsync(
            categories,
            cancellationToken);
        var computerSectionTask = CreateCategorySectionAsync(
            "computer-products",
            rows: 2,
            ComputerSectionDefinitions,
            categories,
            cancellationToken);
        var audioWearableSectionTask = CreateCategorySectionAsync(
            "audio-wearable-products",
            rows: 1,
            AudioWearableSectionDefinitions,
            categories,
            cancellationToken);
        var tvSectionTask = CreateCategorySectionAsync(
            "tv-products",
            rows: 1,
            TvSectionDefinitions,
            categories,
            cancellationToken,
            enableTabSwitching: false);
        var applianceShowcaseTask = CreateApplianceShowcaseAsync(categories, cancellationToken);

        await Task.WhenAll(
            phoneTabletSectionTask,
            computerSectionTask,
            audioWearableSectionTask,
            tvSectionTask,
            applianceShowcaseTask);

        var phoneTabletSection = await phoneTabletSectionTask;
        var computerSection = await computerSectionTask;
        var audioWearableSection = await audioWearableSectionTask;
        var tvSection = await tvSectionTask;
        var applianceShowcase = await applianceShowcaseTask;
        var accessoryDirectory = CreateAccessoryDirectory(categories);

        return new HomeIndexViewModel
        {
            Hero = HomeHeroViewModelFactory.Create(categoryMenu.Items),
            FeaturedCategorySections = new[] { phoneTabletSection }
                .Where(HasRenderableTabs)
                .ToList(),
            AccessoryDirectory = accessoryDirectory,
            AdditionalCategorySections = new[]
                {
                    computerSection,
                    audioWearableSection,
                    tvSection
                }
                .Where(HasRenderableTabs)
                .ToList(),
            ApplianceShowcase = applianceShowcase
        };
    }

    private async Task<string> GetCategoryCacheTokenAsync(CancellationToken cancellationToken)
    {
        await dbQueryGate.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await CategoryCacheTokenBuilder.CreateAsync(dbContext, cancellationToken);
        }
        finally
        {
            dbQueryGate.Release();
        }
    }

    private async Task<HomeApplianceShowcaseViewModel> CreateApplianceShowcaseAsync(
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        if (!HasActiveCategory([HomeApplianceShowcaseContent.RootCategorySlug], categories))
        {
            return new HomeApplianceShowcaseViewModel
            {
                Id = HomeApplianceShowcaseContent.Id,
                Title = HomeApplianceShowcaseContent.Title,
                ViewAllUrl = BuildCatalogUrl(HomeApplianceShowcaseContent.RootCategorySlug),
                HeaderLinks = [],
                Columns = []
            };
        }

        var brands = await GetBrandsByCategoryAsync(
            [HomeApplianceShowcaseContent.RootCategorySlug],
            HomeApplianceShowcaseContent.RootCategorySlug,
            categories,
            cancellationToken);
        var headerLinks = brands
            .Select(brand => new CategoryDirectoryLinkViewModel
            {
                Label = brand.Label,
                Url = brand.Url
            })
            .ToList();

        var columns = HomeApplianceShowcaseContent.Groups
            .Select(group => CreateApplianceShowcaseColumn(group, categories))
            .OfType<HomeApplianceShowcaseColumnViewModel>()
            .ToList();

        return new HomeApplianceShowcaseViewModel
        {
            Id = HomeApplianceShowcaseContent.Id,
            Title = HomeApplianceShowcaseContent.Title,
            ViewAllUrl = BuildCatalogUrl(HomeApplianceShowcaseContent.RootCategorySlug),
            HeaderLinks = headerLinks,
            Columns = columns
        };
    }

    private static HomeApplianceShowcaseColumnViewModel? CreateApplianceShowcaseColumn(
        HomeApplianceShowcaseGroupDefinition group,
        IReadOnlyList<CategoryRecord> categories)
    {
        var sectionCategories = group.SectionSlugs
            .Select(slug => categories.FirstOrDefault(category =>
                string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase)))
            .OfType<CategoryRecord>()
            .ToList();
        if (sectionCategories.Count == 0)
        {
            return null;
        }

        var sectionIds = sectionCategories
            .Select(category => category.Id)
            .ToHashSet();
        var items = categories
            .Where(category => category.ParentId.HasValue && sectionIds.Contains(category.ParentId.Value))
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryDirectoryItemViewModel
            {
                Label = category.Name,
                Url = BuildCatalogUrl(category.Slug),
                ImageUrl = NormalizeCategoryImage(category.ImagePath),
                ImageAlt = category.Name
            })
            .ToList();
        if (items.Count == 0)
        {
            return null;
        }

        var primarySection = sectionCategories[0];
        var viewAllUrl = sectionCategories.Count == 1
            ? BuildCatalogUrl(primarySection.Slug)
            : BuildCatalogUrl(HomeApplianceShowcaseContent.RootCategorySlug);

        return new HomeApplianceShowcaseColumnViewModel
        {
            Id = group.Id,
            Title = group.Title,
            ViewAllUrl = viewAllUrl,
            BannerUrl = BuildCatalogUrl(primarySection.Slug),
            BannerImageUrl = group.BannerImageUrl,
            BannerImageAlt = group.Title,
            Items = items
                .Take(HomeApplianceShowcaseContent.VisibleItemCount)
                .ToList()
        };
    }

    private static CategoryDirectoryViewModel CreateAccessoryDirectory(
        IReadOnlyList<CategoryRecord> categories)
    {
        if (!HasActiveCategory(["phu-kien", "accessories"], categories))
        {
            return new CategoryDirectoryViewModel
            {
                Id = "db-accessory-directory",
                Title = HomeAccessoryDirectoryContent.Title,
                ViewAllUrl = BuildCatalogUrl("phu-kien"),
                Items = []
            };
        }

        var categoriesBySlug = categories.ToDictionary(
            category => category.Slug,
            StringComparer.OrdinalIgnoreCase);

        var items = HomeAccessoryDirectoryContent.Categories
            .Select(definition =>
            {
                if (!categoriesBySlug.TryGetValue(definition.DbCategorySlug, out var category))
                {
                    return null;
                }

                return new CategoryDirectoryItemViewModel
                {
                    Label = category.Name,
                    Url = BuildCatalogUrl(category.Slug),
                    ImageUrl = string.IsNullOrWhiteSpace(category.ImagePath)
                        ? HomeAccessoryDirectoryContent.GetMockImageUrl(definition)
                        : NormalizeCategoryImage(category.ImagePath),
                    ImageAlt = category.Name
                };
            })
            .OfType<CategoryDirectoryItemViewModel>()
            .ToList();

        return new CategoryDirectoryViewModel
        {
            Id = "db-accessory-directory",
            Title = HomeAccessoryDirectoryContent.Title,
            ViewAllUrl = BuildCatalogUrl("phu-kien"),
            Items = items
        };
    }

    private async Task<CategoryProductsViewModel> CreatePhoneTabletSectionAsync(
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        var hasPhoneCategory = HasActiveCategory(PhoneCategorySlugs, categories);
        var hasTabletCategory = HasActiveCategory(TabletCategorySlugs, categories);
        var phoneProductsTask = hasPhoneCategory
            ? GetVariantCardsByCategoryAsync(
                PhoneCategorySlugs,
                categories,
                cancellationToken)
            : Task.FromResult<IReadOnlyList<ProductCardViewModel>>([]);
        var tabletProductsTask = hasTabletCategory
            ? GetVariantCardsByCategoryAsync(
                TabletCategorySlugs,
                categories,
                cancellationToken)
            : Task.FromResult<IReadOnlyList<ProductCardViewModel>>([]);
        var phoneBrandsTask = hasPhoneCategory
            ? GetBrandsByCategoryAsync(
                PhoneCategorySlugs,
                "phone",
                categories,
                cancellationToken)
            : Task.FromResult<IReadOnlyList<CategoryBrandViewModel>>([]);
        var tabletBrandsTask = hasTabletCategory
            ? GetBrandsByCategoryAsync(
                TabletCategorySlugs,
                "tablet",
                categories,
                cancellationToken)
            : Task.FromResult<IReadOnlyList<CategoryBrandViewModel>>([]);

        await Task.WhenAll(
            phoneProductsTask,
            tabletProductsTask,
            phoneBrandsTask,
            tabletBrandsTask);

        var phoneProducts = await phoneProductsTask;
        var tabletProducts = await tabletProductsTask;
        var phoneBrands = await phoneBrandsTask;
        var tabletBrands = await tabletBrandsTask;
        var tabs = new List<CategoryTabViewModel>();

        if (hasPhoneCategory)
        {
            tabs.Add(new CategoryTabViewModel
            {
                Id = "phones",
                Label = "Điện thoại",
                Url = "/catalog?cat=phone",
                IsActive = true,
                Panel = new CategoryProductPanelViewModel
                {
                    ViewAllUrl = "/catalog?cat=phone",
                    Banners = HomePhoneTabletSectionContent.CreatePhoneBanners(),
                    QuickLinks = HomePhoneTabletSectionContent.CreatePhoneQuickLinks(),
                    Brands = phoneBrands,
                    Products = phoneProducts
                }
            });
        }

        if (hasTabletCategory)
        {
            tabs.Add(new CategoryTabViewModel
            {
                Id = "tablets",
                Label = "Máy tính bảng",
                Url = "/catalog?cat=tablet",
                IsActive = tabs.Count == 0,
                Panel = new CategoryProductPanelViewModel
                {
                    ViewAllUrl = "/catalog?cat=tablet",
                    Banners = HomePhoneTabletSectionContent.CreateTabletBanners(),
                    QuickLinks = HomePhoneTabletSectionContent.CreateTabletQuickLinks(),
                    Brands = tabletBrands,
                    Products = tabletProducts
                }
            });
        }

        return new CategoryProductsViewModel
        {
            Id = "phone-products",
            Rows = 2,
            EnableTabSwitching = true,
            ShowPagination = false,
            Tabs = tabs
        };
    }

    private async Task<CategoryProductsViewModel> CreateCategorySectionAsync(
        string sectionId,
        int rows,
        IReadOnlyList<CategorySectionDefinition> definitions,
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken,
        bool enableTabSwitching = true)
    {
        var activeDefinitions = definitions
            .Where(definition => HasActiveCategory(
                ResolveCategorySlugs(definition.CategorySlug),
                categories))
            .ToArray();

        var tabTasks = activeDefinitions.Select(async definition =>
        {
            var categorySlugs = ResolveCategorySlugs(definition.CategorySlug);
            var productsTask = GetVariantCardsByCategoryAsync(
                categorySlugs,
                categories,
                cancellationToken);
            var brandsTask = definition.ShowBrandFilter
                ? GetBrandsByCategoryAsync(
                    categorySlugs,
                    definition.CategorySlug,
                    categories,
                    cancellationToken)
                : Task.FromResult<IReadOnlyList<CategoryBrandViewModel>>([]);

            await Task.WhenAll(productsTask, brandsTask);

            var products = await productsTask;
            var brands = await brandsTask;
            var quickLinks = BuildCategoryQuickLinks(
                definition.CategorySlug,
                categories);

            if (IsTvCategory(definition.CategorySlug))
            {
                if (products.Count == 0)
                {
                    products = HomeTvCategorySectionContent.CreateProducts();
                }

                if (brands.Count == 0)
                {
                    brands = HomeTvCategorySectionContent.CreateBrands();
                }
            }

            return new CategoryTabViewModel
            {
                Id = definition.Id,
                Label = definition.Label,
                Url = BuildCatalogUrl(definition.CategorySlug),
                IsActive = definition.IsActive,
                Panel = new CategoryProductPanelViewModel
                {
                    ViewAllUrl = BuildCatalogUrl(definition.CategorySlug),
                    Banners = HomeAdditionalCategorySectionContent.CreateBanners(
                        definition.CategorySlug),
                    QuickLinks = quickLinks,
                    Brands = brands,
                    Products = products
                }
            };
        }).ToArray();
        var tabs = await Task.WhenAll(tabTasks);

        return new CategoryProductsViewModel
        {
            Id = sectionId,
            Rows = rows,
            EnableTabSwitching = enableTabSwitching,
            ShowPagination = false,
            Tabs = tabs
        };
    }

    private async Task<IReadOnlyList<ProductCardViewModel>> GetVariantCardsByCategoryAsync(
        IReadOnlyCollection<string> categorySlugs,
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"home-product-cards-v3:{string.Join(',', categorySlugs.OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase))}";

        try
        {
            var cards = await cache.GetOrCreateExclusiveAsync(
                cacheKey,
                () => LoadVariantCardsByCategoryAsync(
                    categorySlugs,
                    categories,
                    CancellationToken.None),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });

            return cards ?? [];
        }
        catch (Exception exception) when (ContainsSqlTimeout(exception))
        {
            logger.LogWarning(
                exception,
                "Timed out while loading homepage products for {CategorySlugs}. Returning an empty panel instead of failing the page.",
                string.Join(", ", categorySlugs));
            return [];
        }
    }

    private async Task<IReadOnlyList<ProductCardViewModel>> LoadVariantCardsByCategoryAsync(
        IReadOnlyCollection<string> categorySlugs,
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        var categoryIds = GetCategoryTreeIds(categorySlugs, categories);
        if (categoryIds.Count == 0)
        {
            return [];
        }

        await dbQueryGate.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var variants = await dbContext.ProductVariants
                .AsNoTracking()
                .Include(variant => variant.Product)
                .Where(variant => variant.IsActive)
                .Where(variant => variant.Product != null && variant.Product.IsActive)
                .Where(variant => categoryIds.Contains(variant.Product!.CategoryId))
                .OrderByDescending(variant => variant.Product!.IsFeatured)
                .ThenByDescending(variant => variant.SoldCount)
                .ThenByDescending(variant => variant.Product!.TotalSoldCount)
                .ThenByDescending(variant => variant.IsDefault)
                .ThenByDescending(variant => variant.CreatedAt)
                .Take(MaxVariantsPerPanel)
                .ToListAsync(cancellationToken);

            var variantIds = variants
                .Select(variant => variant.Id)
                .ToArray();
            if (variantIds.Length == 0)
            {
                return [];
            }

            // Keep the homepage query small: collection data is fetched only for the ten selected variants.
            var images = await dbContext.ProductVariantImages
                .AsNoTracking()
                .Where(image => variantIds.Contains(image.ProductVariantId))
                .OrderBy(image => image.ProductVariantId)
                .ThenBy(image => image.Position)
                .ThenBy(image => image.Id)
                .ToListAsync(cancellationToken);
            var attributes = await dbContext.VariantAttributes
                .AsNoTracking()
                .Where(attribute => variantIds.Contains(attribute.ProductVariantId))
                .Include(attribute => attribute.AttributeOption)
                    .ThenInclude(option => option!.Attribute)
                .ToListAsync(cancellationToken);
            var firstImageByVariantId = images
                .GroupBy(image => image.ProductVariantId)
                .ToDictionary(group => group.Key, group => group.First());
            var attributesByVariantId = attributes
                .GroupBy(attribute => attribute.ProductVariantId)
                .ToDictionary(group => group.Key, group => (ICollection<VariantAttribute>)group.ToList());

            foreach (var variant in variants)
            {
                variant.ProductVariantImages = firstImageByVariantId.TryGetValue(variant.Id, out var image)
                    ? [image]
                    : [];
                variant.VariantAttributes = attributesByVariantId.TryGetValue(variant.Id, out var variantAttributes)
                    ? variantAttributes
                    : [];
            }

            return variants
                .Select(DbHomeProductCardMapper.ToProductCard)
                .OfType<ProductCardViewModel>()
                .ToList();
        }
        finally
        {
            dbQueryGate.Release();
        }
    }

    private async Task<IReadOnlyList<CategoryBrandViewModel>> GetBrandsByCategoryAsync(
        IReadOnlyCollection<string> categorySlugs,
        string categoryQueryValue,
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        var categoryIds = GetCategoryTreeIds(categorySlugs, categories);
        if (categoryIds.Count == 0)
        {
            return [];
        }

        await dbQueryGate.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var brands = await dbContext.Products
                .AsNoTracking()
                .Where(product => product.IsActive)
                .Where(product => categoryIds.Contains(product.CategoryId))
                .Where(product => product.Brand != null && product.Brand.IsActive)
                .Where(product => product.ProductVariants.Any(variant => variant.IsActive))
                .Select(product => new
                {
                    product.Brand!.Name,
                    product.Brand.Slug
                })
                .Distinct()
                .OrderBy(brand => brand.Name)
                .ToListAsync(cancellationToken);

            return brands
                .Select(brand => new CategoryBrandViewModel
                {
                    Label = brand.Name,
                    Url = $"/catalog?cat={categoryQueryValue}&brand={Uri.EscapeDataString(GetBrandUrlValue(brand.Slug, brand.Name))}"
                })
                .ToList();
        }
        finally
        {
            dbQueryGate.Release();
        }
    }

    private async Task<IReadOnlyList<CategoryRecord>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken)
    {
        await dbQueryGate.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.Position)
                .ThenBy(category => category.Id)
                .Select(category => new CategoryRecord(
                    category.Id,
                    category.ParentId,
                    category.Name,
                    category.Slug,
                    category.ImagePath,
                    category.Position))
                .ToListAsync(cancellationToken);
        }
        finally
        {
            dbQueryGate.Release();
        }
    }

    private static bool ContainsSqlTimeout(Exception exception)
    {
        return exception is SqlException { Number: -2 }
            || exception.InnerException is not null && ContainsSqlTimeout(exception.InnerException);
    }

    private static IReadOnlyList<CategoryQuickLinkViewModel> BuildCategoryQuickLinks(
        string rootCategorySlug,
        IReadOnlyList<CategoryRecord> categories)
    {
        var quickLinks = BuildLevelTwoCategoryLinks(
            ResolveCategorySlugs(rootCategorySlug),
            categories);

        return quickLinks.Count > 0
            ? quickLinks
            : IsTvCategory(rootCategorySlug)
                ? HomeTvCategorySectionContent.CreateQuickLinks()
                : quickLinks;
    }

    private static IReadOnlyList<CategoryQuickLinkViewModel> BuildLevelTwoCategoryLinks(
        IReadOnlyCollection<string> rootCategorySlugs,
        IReadOnlyList<CategoryRecord> categories)
    {
        var normalizedSlugs = rootCategorySlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rootCategoryIds = categories
            .Where(category => category.ParentId is null
                && normalizedSlugs.Contains(category.Slug))
            .Select(category => category.Id)
            .ToHashSet();

        if (rootCategoryIds.Count == 0)
        {
            return [];
        }

        return categories
            .Where(category => category.ParentId.HasValue
                && rootCategoryIds.Contains(category.ParentId.Value))
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryQuickLinkViewModel
            {
                Label = category.Name,
                Url = BuildCatalogUrl(category.Slug),
                ImageUrl = NormalizeCategoryImage(category.ImagePath)
            })
            .ToList();
    }

    private static HashSet<long> GetCategoryTreeIds(
        IReadOnlyCollection<string> rootSlugs,
        IReadOnlyList<CategoryRecord> categories)
    {
        var normalizedSlugs = rootSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoryIds = categories
            .Where(category => normalizedSlugs.Contains(category.Slug))
            .Select(category => category.Id)
            .ToHashSet();

        var addedChild = true;
        while (addedChild)
        {
            addedChild = false;
            foreach (var category in categories)
            {
                if (category.ParentId.HasValue
                    && categoryIds.Contains(category.ParentId.Value)
                    && categoryIds.Add(category.Id))
                {
                    addedChild = true;
                }
            }
        }

        return categoryIds;
    }

    private static string BuildCatalogUrl(string categorySlug)
    {
        return $"/catalog?cat={Uri.EscapeDataString(categorySlug)}";
    }

    private static string NormalizeCategoryImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return CategoryFallbackImage;
        }

        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith('/'))
        {
            return imagePath;
        }

        return "/" + imagePath.TrimStart('/');
    }

    private static string GetBrandUrlValue(string? slug, string name)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? name.ToLowerInvariant()
            : slug;
    }

    private static bool HasRenderableTabs(CategoryProductsViewModel section)
    {
        return section.Tabs.Any(tab => tab.Panel is not null);
    }

    private static IReadOnlyCollection<string> ResolveCategorySlugs(string categorySlug)
    {
        return IsTvCategory(categorySlug)
            ? TvCategorySlugs
            : [categorySlug];
    }

    private static bool HasActiveCategory(
        IReadOnlyCollection<string> categorySlugs,
        IReadOnlyList<CategoryRecord> categories)
    {
        var normalizedSlugs = categorySlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return categories.Any(category => normalizedSlugs.Contains(category.Slug));
    }

    private static bool IsTvCategory(string categorySlug)
    {
        return string.Equals(categorySlug, "tv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(categorySlug, "tivi", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CategorySectionDefinition(
        string Id,
        string Label,
        string CategorySlug,
        bool IsActive = false,
        bool ShowBrandFilter = false);

    private sealed record CategoryRecord(
        long Id,
        long? ParentId,
        string Name,
        string Slug,
        string? ImagePath,
        int Position);
}
