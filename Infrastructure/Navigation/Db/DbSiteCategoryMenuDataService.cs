using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Navigation.Db;

public sealed class DbSiteCategoryMenuDataService(EcommerceDbContext dbContext) : ISiteCategoryMenuDataService
{
    private static readonly CategoryLinkGroupDefinition[] ExplicitCategoryRows =
    [
        new(new[]
        {
            new[] { "dien-thoai", "phone", "smartphone", "dien-thoai-di-dong" },
            new[] { "tablet", "may-tinh-bang" }
        }),
        new(new[]
        {
            new[] { "dong-ho", "watch", "smartwatch" },
            new[] { "camera", "may-anh", "flycam" }
        }),
        new(new[]
        {
            new[] { "pc", "desktop", "may-tinh-de-ban" },
            new[] { "man-hinh", "monitor", "man-hinh-may-tinh" },
            new[] { "may-in", "printer", "may-in-scan" }
        }),
        new(new[]
        {
            new[] { "tivi", "tv" },
            new[] { "dien-may", "home-electronics", "dien-lanh" }
        })
    ];

    private static readonly FilterLinkDefinition[] PhonePriceRanges =
    [
        new("Dưới 2 triệu", "under-2m"),
        new("Từ 2 - 4 triệu", "2m-4m"),
        new("Từ 4 - 7 triệu", "4m-7m"),
        new("Từ 7 - 13 triệu", "7m-13m"),
        new("Từ 13 - 20 triệu", "13m-20m"),
        new("Trên 20 triệu", "over-20m")
    ];

    private static readonly FilterLinkDefinition[] LaptopPriceRanges =
    [
        new("Dưới 10 triệu", "under-10m"),
        new("Từ 10 - 15 triệu", "10m-15m"),
        new("Từ 15 - 20 triệu", "15m-20m"),
        new("Từ 20 - 25 triệu", "20m-25m"),
        new("Từ 25 - 30 triệu", "25m-30m"),
        new("Trên 30 triệu", "over-30m")
    ];

    private static readonly FilterLinkDefinition[] LaptopScreenSizeRanges =
    [
        new("Laptop 13 inch", "13"),
        new("Laptop 14 inch", "14"),
        new("Laptop 15.6 inch", "15.6"),
        new("Laptop 16 inch", "16")
    ];

    private static readonly FilterLinkDefinition[] AudioPriceRanges =
    [
        new("Tai nghe dưới 200K", "under-200k"),
        new("Tai nghe dưới 500K", "under-500k"),
        new("Tai nghe dưới 1 triệu", "under-1m"),
        new("Tai nghe dưới 2 triệu", "under-2m"),
        new("Tai nghe dưới 5 triệu", "under-5m")
    ];

    private static readonly FilterLinkDefinition[] AppliancePriceRanges =
    [
        new("Dưới 500K", "under-500k"),
        new("Từ 500K - 1 triệu", "500k-1m"),
        new("Từ 1 - 3 triệu", "1m-3m"),
        new("Từ 3 - 5 triệu", "3m-5m"),
        new("Từ 5 - 10 triệu", "5m-10m"),
        new("Trên 10 triệu", "over-10m")
    ];

    private static readonly FilterLinkDefinition[] HomeElectronicsPriceRanges =
    [
        new("Dưới 5 triệu", "under-5m"),
        new("Từ 5 - 10 triệu", "5m-10m"),
        new("Từ 10 - 20 triệu", "10m-20m"),
        new("Trên 20 triệu", "over-20m")
    ];

    private static readonly FilterLinkDefinition[] ComputerPriceRanges =
    [
        new("Dưới 10 triệu", "under-10"),
        new("Từ 10 - 20 triệu", "10-20"),
        new("Từ 20 - 30 triệu", "20-30"),
        new("Trên 30 triệu", "over-30")
    ];

    private static readonly FilterLinkDefinition[] PeripheralPriceRanges =
    [
        new("Dưới 1 triệu", "under-1m"),
        new("Từ 1 - 3 triệu", "1m-3m"),
        new("Từ 3 - 5 triệu", "3m-5m"),
        new("Từ 5 - 10 triệu", "5m-10m"),
        new("Trên 10 triệu", "over-10m")
    ];

    private static readonly FilterLinkDefinition[] ComponentPriceRanges =
    [
        new("Dưới 1 triệu", "under-1m"),
        new("Từ 1 - 3 triệu", "1m-3m"),
        new("Từ 3 - 5 triệu", "3m-5m"),
        new("Từ 5 - 10 triệu", "5m-10m"),
        new("Từ 10 - 20 triệu", "10m-20m"),
        new("Trên 20 triệu", "over-20m")
    ];

    private static readonly string[] LaptopChipFallbacks =
    [
        "Laptop Core i3",
        "Laptop Core i5",
        "Laptop Core i7",
        "Laptop Core i9",
        "Laptop Core U5",
        "Laptop Core U7",
        "Intel Core Ultra",
        "Apple M1 Series",
        "Apple M2 Series",
        "Apple M3 Series",
        "Apple M4 Series",
        "Apple M5 Series",
        "AMD Ryzen"
    ];

    private static readonly string[] ApplianceBrandFallbackSlugs =
    [
        "aqua",
        "bosch",
        "camel",
        "casper",
        "daikin",
        "electrolux",
        "hitachi",
        "hoa-phat",
        "lg",
        "panasonic",
        "philips",
        "robot",
        "samsung",
        "sharp",
        "toshiba",
        "xiaomi"
    ];

    public async Task<SiteCategoryMenuViewModel> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
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

        var rootCategories = categories
            .Where(category => category.ParentId is null)
            .OrderBy(GetMenuSortRank)
            .ThenBy(category => category.Position)
            .ThenBy(category => category.Id)
            .ToList();

        var brandRecords = await GetBrandRecordsAsync(cancellationToken);
        var activeBrands = await GetActiveBrandsAsync(cancellationToken);
        var laptopChipValues = await GetLaptopChipValuesAsync(categories, cancellationToken);
        var items = BuildMenuItems(rootCategories, categories, brandRecords, activeBrands, laptopChipValues);

        return new SiteCategoryMenuViewModel
        {
            Items = items
        };
    }

    private async Task<IReadOnlyList<BrandCategoryRecord>> GetBrandRecordsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive
                && product.Brand != null
                && product.Brand.IsActive
                && product.ProductVariants.Any(variant => variant.IsActive))
            .Select(product => new BrandCategoryRecord(
                product.CategoryId,
                product.Brand!.Id,
                product.Brand.Name,
                product.Brand.Slug,
                product.Brand.ImagePath))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ActiveBrandRecord>> GetActiveBrandsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.Name)
            .Select(brand => new ActiveBrandRecord(
                brand.Id,
                brand.Name,
                brand.Slug,
                brand.ImagePath))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetLaptopChipValuesAsync(
        IReadOnlyList<CategoryRecord> categories,
        CancellationToken cancellationToken)
    {
        var laptopCategory = categories.FirstOrDefault(category =>
            MatchesAny(category, "laptop"));
        if (laptopCategory is null)
        {
            return [];
        }

        var laptopCategoryIds = GetCategoryTreeIds([laptopCategory], categories).ToList();
        if (laptopCategoryIds.Count == 0)
        {
            return [];
        }

        var specificationValues = await dbContext.ProductSpecifications
            .AsNoTracking()
            .Where(specification => specification.Product != null
                && specification.Product.IsActive
                && laptopCategoryIds.Contains(specification.Product.CategoryId)
                && specification.Specification != null)
            .Select(specification => new ProductSpecificationRecord(
                specification.Specification!.Key,
                specification.Specification.Name,
                specification.Value))
            .ToListAsync(cancellationToken);

        var chipValues = specificationValues
            .Where(IsLaptopChipSpecification)
            .SelectMany(specification => ResolveChipLabels(specification.Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetChipSortRank)
            .ThenBy(value => value)
            .Take(12)
            .ToList();

        return chipValues;
    }

    private static IReadOnlyList<SiteCategoryMenuItemViewModel> BuildMenuItems(
        IReadOnlyList<CategoryRecord> rootCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        IReadOnlyList<ActiveBrandRecord> activeBrands,
        IReadOnlyList<string> laptopChipValues)
    {
        var items = new List<SiteCategoryMenuItemViewModel>();
        var rowNumber = 1;
        var consumedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in rootCategories)
        {
            if (consumedSlugs.Contains(category.Slug))
            {
                continue;
            }

            var rowCategories = ResolveExplicitRow(category, rootCategories, consumedSlugs);
            if (rowCategories.Count == 0)
            {
                rowCategories = [category];
            }

            items.Add(ToMenuItem(
                rowNumber,
                rowCategories,
                categories,
                brandRecords,
                activeBrands,
                laptopChipValues));

            foreach (var rowCategory in rowCategories)
            {
                consumedSlugs.Add(rowCategory.Slug);
            }

            rowNumber++;
        }

        return items;
    }

    private static SiteCategoryMenuItemViewModel ToMenuItem(
        int rowNumber,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        IReadOnlyList<ActiveBrandRecord> activeBrands,
        IReadOnlyList<string> laptopChipValues)
    {
        return new SiteCategoryMenuItemViewModel
        {
            Id = BuildItemId(rowNumber, rowCategories),
            Url = BuildCatalogUrl(rowCategories[0].Slug),
            Label = string.Join(", ", rowCategories.Select(category => category.Name)),
            Icon = ResolveIcon(rowCategories),
            IsHighlighted = IsHighlighted(rowCategories),
            CategoryLinks = rowCategories
                .Select(category => new SiteCategoryMenuLinkViewModel
                {
                    Label = category.Name,
                    Url = BuildCatalogUrl(category.Slug)
                })
                .ToList(),
            Groups = BuildGroups(rowCategories, categories, brandRecords, activeBrands, laptopChipValues)
        };
    }

    private static IReadOnlyList<SiteCategoryMenuGroupViewModel> BuildGroups(
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        IReadOnlyList<ActiveBrandRecord> activeBrands,
        IReadOnlyList<string> laptopChipValues)
    {
        var groups = new List<SiteCategoryMenuGroupViewModel>();
        var firstSlug = rowCategories[0].Slug;

        if (HasCategory(rowCategories, "dien-thoai", "phone", "smartphone", "tablet", "may-tinh-bang"))
        {
            AddBrandGroupsForRows(groups, rowCategories, categories, brandRecords, "Hãng");
            AddCategoryGroupsForRows(groups, rowCategories, categories, includeImages: false, take: 14);
            AddIfNotNull(groups, BuildFilterGroup("Mức giá điện thoại", firstSlug, "price", PhonePriceRanges));
        }
        else if (HasCategory(rowCategories, "laptop"))
        {
            var categoryIds = GetCategoryTreeIds(rowCategories, categories);
            AddIfNotNull(groups, WithGroupClass(
                BuildBrandGroup("Thương hiệu", firstSlug, categoryIds, brandRecords, take: 12),
                "site-category-mega-group--laptop site-category-mega-group--laptop-brands"));
            AddIfNotNull(groups, WithGroupClass(
                BuildChildCategoryGroup(
                    "Nhu cầu sử dụng",
                    rowCategories,
                    categories,
                    includeImages: true,
                    take: 8),
                "site-category-mega-group--laptop site-category-mega-group--laptop-usage"));
            AddIfNotNull(groups, WithGroupClass(
                BuildChipGroup(firstSlug, laptopChipValues),
                "site-category-mega-group--laptop site-category-mega-group--laptop-chip"));
            AddIfNotNull(groups, WithGroupClass(
                BuildFilterGroup("Kích thước màn hình", firstSlug, "screen-size", LaptopScreenSizeRanges),
                "site-category-mega-group--laptop site-category-mega-group--laptop-screen"));
            AddIfNotNull(groups, WithGroupClass(
                BuildFilterGroup("Phân khúc giá", firstSlug, "price", LaptopPriceRanges),
                "site-category-mega-group--laptop site-category-mega-group--laptop-price"));
        }
        else if (HasCategory(rowCategories, "am-thanh", "audio", "mic", "tai-nghe", "loa"))
        {
            AddAudioGroups(groups, rowCategories, categories);
            AddAudioBrandGroups(groups, rowCategories, categories, brandRecords);
            AddIfNotNull(groups, WithGroupClass(
                BuildFilterGroup("Chọn theo giá", firstSlug, "price", AudioPriceRanges),
                "site-category-mega-group--audio site-category-mega-group--audio-price"));
        }
        else if (HasCategory(rowCategories, "dong-ho", "watch", "smartwatch", "camera", "may-anh"))
        {
            AddCategoryGroupsForRows(groups, rowCategories, categories, includeImages: true, take: 10);
            AddBrandGroupsForRows(groups, rowCategories, categories, brandRecords, "Chọn theo thương hiệu");
        }
        else if (HasCategory(rowCategories, "do-gia-dung", "gia-dung", "lam-dep", "suc-khoe", "appliance"))
        {
            var categoryIds = GetCategoryTreeIds(rowCategories, categories);
            var brandGroup = WithGroupClass(
                BuildBrandGroup("Thương hiệu", firstSlug, categoryIds, brandRecords, take: 16)
                ?? BuildBrandFallbackGroup(
                    "Thương hiệu",
                    firstSlug,
                    activeBrands,
                    ApplianceBrandFallbackSlugs,
                    take: ApplianceBrandFallbackSlugs.Length),
                "site-category-mega-group--appliance-brands");
            var priceGroup = WithGroupClass(
                BuildFilterGroup("Mức giá", firstSlug, "price", AppliancePriceRanges),
                "site-category-mega-group--appliance-price");

            AddApplianceGroups(groups, rowCategories, categories, brandGroup, priceGroup);
        }
        else if (HasCategory(rowCategories, "phu-kien-may-tinh", "linh-kien-may-tinh", "computer-accessories"))
        {
            AddComputerAccessoryGroups(groups, rowCategories, categories, brandRecords);
        }
        else if (HasCategory(rowCategories, "phu-kien", "accessory", "cable", "cap-sac"))
        {
            AddAccessoryGroups(groups, rowCategories, categories);
        }
        else if (HasCategory(rowCategories, "pc", "desktop", "may-tinh", "man-hinh", "monitor", "may-in", "printer"))
        {
            AddPcMonitorPrinterGroups(groups, rowCategories, categories, brandRecords);
        }
        else if (HasCategory(rowCategories, "tivi", "tv", "dien-may", "home-electronics", "dien-lanh"))
        {
            AddHomeElectronicsGroups(groups, rowCategories, categories, brandRecords);
        }
        else
        {
            AddCategoryGroupsForRows(groups, rowCategories, categories, includeImages: true, take: 10);
            AddIfNotNull(groups, BuildBrandGroup(
                "Thương hiệu",
                firstSlug,
                GetCategoryTreeIds(rowCategories, categories),
                brandRecords,
                take: 12));
        }

        if (groups.Count == 0)
        {
            groups.Add(new SiteCategoryMenuGroupViewModel
            {
                Title = "Danh mục",
                Links = rowCategories
                    .Select(category => new SiteCategoryMenuLinkViewModel
                    {
                        Label = category.Name,
                        Url = BuildCatalogUrl(category.Slug)
                    })
                    .ToList()
            });
        }

        return groups;
    }

    private static void AddCategoryGroupsForRows(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        bool includeImages,
        int take)
    {
        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, BuildChildCategoryGroup(
                ResolveChildGroupTitle(rowCategory),
                [rowCategory],
                categories,
                includeImages,
                take));
        }
    }

    private static void AddBrandGroupsForRows(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        string titlePrefix)
    {
        foreach (var rowCategory in rowCategories)
        {
            var title = rowCategories.Count == 1
                ? titlePrefix
                : $"{titlePrefix} {rowCategory.Name.ToLowerInvariant()}";
            AddIfNotNull(groups, BuildBrandGroup(
                title,
                rowCategory.Slug,
                GetCategoryTreeIds([rowCategory], categories),
                brandRecords,
                take: 12));
        }
    }

    private static void AddAccessoryGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories)
    {
        foreach (var group in BuildNestedCategoryGroups(rowCategories, categories))
        {
            groups.Add(group.Group);
        }
    }

    private static void AddAudioGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories)
    {
        foreach (var group in BuildNestedCategoryGroups(rowCategories, categories))
        {
            groups.Add(WithGroupClass(
                group.Group,
                $"site-category-mega-group--audio {ResolveAudioGroupClass(group.LevelTwoCategory)}")!);
        }
    }

    private static void AddAudioBrandGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords)
    {
        AddAudioBrandGroup(
            groups,
            "Hãng tai nghe",
            "tai-nghe",
            rowCategories,
            categories,
            brandRecords,
            "site-category-mega-group--audio-headphone-brands");
        AddAudioBrandGroup(
            groups,
            "Hãng loa",
            "loa",
            rowCategories,
            categories,
            brandRecords,
            "site-category-mega-group--audio-speaker-brands");
    }

    private static void AddAudioBrandGroup(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        string title,
        string categorySlug,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        string cssClass)
    {
        var category = ResolveAudioCategory(rowCategories, categories, categorySlug);
        if (category is null)
        {
            return;
        }

        AddIfNotNull(groups, WithGroupClass(
            BuildBrandGroup(
                title,
                category.Slug,
                GetCategoryTreeIds([category], categories),
                brandRecords,
                take: 12),
            $"site-category-mega-group--audio {cssClass}"));
    }

    private static void AddComputerAccessoryGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords)
    {
        var categoryIds = GetCategoryTreeIds(rowCategories, categories);
        var categorySlug = rowCategories[0].Slug;

        AddIfNotNull(groups, WithGroupClass(
            BuildChildCategoryGroup(
                "Danh mục phụ kiện máy tính",
                rowCategories,
                categories,
                includeImages: true,
                take: 12),
            "site-category-mega-group--computer site-category-mega-group--computer-categories"));
        AddIfNotNull(groups, WithGroupClass(
            BuildBrandGroup("Thương hiệu", categorySlug, categoryIds, brandRecords, take: 12),
            "site-category-mega-group--computer site-category-mega-group--computer-brands"));
        AddIfNotNull(groups, WithGroupClass(
            BuildFilterGroup("Mức giá", categorySlug, "price", ComponentPriceRanges),
            "site-category-mega-group--computer site-category-mega-group--computer-price"));
    }

    private static void AddPcMonitorPrinterGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords)
    {
        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, WithGroupClass(
                BuildChildCategoryGroup(
                    ResolveChildGroupTitle(rowCategory),
                    [rowCategory],
                    categories,
                    includeImages: true,
                    take: 10),
                "site-category-mega-group--computer site-category-mega-group--computer-categories"));
        }

        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, WithGroupClass(
                BuildBrandGroup(
                    ResolveBrandGroupTitle(rowCategory),
                    rowCategory.Slug,
                    GetCategoryTreeIds([rowCategory], categories),
                    brandRecords,
                    take: 12),
                "site-category-mega-group--computer site-category-mega-group--computer-brands"));
        }

        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, WithGroupClass(
                BuildFilterGroup(
                    ResolvePriceGroupTitle(rowCategory),
                    rowCategory.Slug,
                    "price",
                    ResolveComputerPriceRanges(rowCategory)),
                "site-category-mega-group--computer site-category-mega-group--computer-price"));
        }
    }

    private static void AddApplianceGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        SiteCategoryMenuGroupViewModel? brandGroup,
        SiteCategoryMenuGroupViewModel? priceGroup)
    {
        var nestedGroups = BuildNestedCategoryGroups(rowCategories, categories).ToList();

        AddGroupBySlug("thiet-bi-gia-dinh", "site-category-mega-group--appliance-home");
        AddGroupBySlug("suc-khoe-lam-dep", "site-category-mega-group--appliance-beauty");
        AddIfNotNull(groups, priceGroup);
        AddIfNotNull(groups, brandGroup);
        AddGroupBySlug("gia-dung-nha-bep", "site-category-mega-group--appliance-kitchen");

        foreach (var group in nestedGroups)
        {
            groups.Add(group.Group);
        }

        void AddGroupBySlug(string slug, string cssClass)
        {
            var group = nestedGroups.FirstOrDefault(item =>
                string.Equals(item.LevelTwoCategory.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (group is null)
            {
                return;
            }

            groups.Add(WithGroupClass(group.Group, cssClass)!);
            nestedGroups.Remove(group);
        }
    }

    private static void AddHomeElectronicsGroups(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        IReadOnlyList<BrandCategoryRecord> brandRecords)
    {
        foreach (var rowCategory in rowCategories)
        {
            if (IsTvCategory(rowCategory))
            {
                AddIfNotNull(groups, WithGroupClass(
                    BuildChildCategoryGroup(
                        rowCategory.Name,
                        [rowCategory],
                        categories,
                        includeImages: true,
                        take: 16),
                    "site-category-mega-group--electronics site-category-mega-group--electronics-tv"));

                continue;
            }

            foreach (var group in BuildNestedCategoryGroups([rowCategory], categories))
            {
                groups.Add(WithGroupClass(
                    group.Group,
                    $"site-category-mega-group--electronics {ResolveHomeElectronicsGroupClass(group.LevelTwoCategory)}")!);
            }
        }

        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, WithGroupClass(
                BuildBrandGroup(
                    ResolveHomeElectronicsBrandGroupTitle(rowCategory),
                    rowCategory.Slug,
                    GetCategoryTreeIds([rowCategory], categories),
                    brandRecords,
                    take: 12),
                "site-category-mega-group--electronics site-category-mega-group--electronics-brands"));
        }

        foreach (var rowCategory in rowCategories)
        {
            AddIfNotNull(groups, WithGroupClass(
                BuildFilterGroup(
                    ResolveHomeElectronicsPriceGroupTitle(rowCategory),
                    rowCategory.Slug,
                    "price",
                    HomeElectronicsPriceRanges),
                "site-category-mega-group--electronics site-category-mega-group--electronics-price"));
        }
    }

    private static IReadOnlyList<NestedCategoryGroupRecord> BuildNestedCategoryGroups(
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories)
    {
        var level2Categories = rowCategories
            .SelectMany(category => GetDirectChildren(category, categories))
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .ToList();
        var groups = new List<NestedCategoryGroupRecord>();

        foreach (var level2Category in level2Categories)
        {
            var level3Categories = GetDirectChildren(level2Category, categories);
            if (level3Categories.Count > 0)
            {
                groups.Add(new NestedCategoryGroupRecord(level2Category, new SiteCategoryMenuGroupViewModel
                {
                    Title = level2Category.Name,
                    Links = level3Categories
                        .Select(category => CreateCategoryLink(category, includeImage: true))
                        .ToList()
                }));
            }
            else
            {
                groups.Add(new NestedCategoryGroupRecord(level2Category, new SiteCategoryMenuGroupViewModel
                {
                    Title = level2Category.Name,
                    Links =
                    [
                        CreateCategoryLink(level2Category, includeImage: false)
                    ]
                }));
            }
        }

        return groups;
    }

    private static SiteCategoryMenuGroupViewModel? BuildChildCategoryGroup(
        string title,
        IReadOnlyList<CategoryRecord> parentCategories,
        IReadOnlyList<CategoryRecord> categories,
        bool includeImages,
        int take)
    {
        var parentIds = parentCategories.Select(category => category.Id).ToHashSet();
        var children = categories
            .Where(category => category.ParentId is long parentId && parentIds.Contains(parentId))
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .Take(take)
            .Select(category => CreateCategoryLink(category, includeImages))
            .ToList();

        return children.Count == 0
            ? null
            : new SiteCategoryMenuGroupViewModel
            {
                Title = title,
                Links = children
            };
    }

    private static SiteCategoryMenuGroupViewModel? BuildBrandGroup(
        string title,
        string categorySlug,
        IReadOnlySet<long> categoryIds,
        IReadOnlyList<BrandCategoryRecord> brandRecords,
        int take)
    {
        var links = brandRecords
            .Where(record => categoryIds.Contains(record.CategoryId))
            .GroupBy(record => record.BrandId)
            .Select(group => group.First())
            .OrderBy(record => record.Name)
            .Take(take)
            .Select(record =>
            {
                var imageUrl = NormalizeOptionalImageUrl(record.ImagePath);
                return new SiteCategoryMenuLinkViewModel
                {
                    Label = record.Name,
                    Url = BuildCatalogUrl(
                        categorySlug,
                        ("brand", GetBrandUrlValue(record.Slug, record.Name))),
                    ImageUrl = imageUrl,
                    ImageAlt = $"Logo {record.Name}",
                    ImageOnly = !string.IsNullOrWhiteSpace(imageUrl)
                };
            })
            .ToList();

        return links.Count == 0
            ? null
            : new SiteCategoryMenuGroupViewModel
            {
                Title = title,
                Links = links
            };
    }

    private static SiteCategoryMenuGroupViewModel? BuildBrandFallbackGroup(
        string title,
        string categorySlug,
        IReadOnlyList<ActiveBrandRecord> activeBrands,
        IReadOnlyList<string> brandSlugs,
        int take)
    {
        var allowedSlugs = brandSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var links = activeBrands
            .Where(brand => allowedSlugs.Contains(brand.Slug))
            .OrderBy(brand => Array.FindIndex(
                brandSlugs.ToArray(),
                slug => string.Equals(slug, brand.Slug, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(brand => brand.Name)
            .Take(take)
            .Select(brand =>
            {
                var imageUrl = NormalizeOptionalImageUrl(brand.ImagePath);
                return new SiteCategoryMenuLinkViewModel
                {
                    Label = brand.Name,
                    Url = BuildCatalogUrl(
                        categorySlug,
                        ("brand", GetBrandUrlValue(brand.Slug, brand.Name))),
                    ImageUrl = imageUrl,
                    ImageAlt = $"Logo {brand.Name}",
                    ImageOnly = !string.IsNullOrWhiteSpace(imageUrl)
                };
            })
            .ToList();

        return links.Count == 0
            ? null
            : new SiteCategoryMenuGroupViewModel
            {
                Title = title,
                Links = links
            };
    }

    private static SiteCategoryMenuGroupViewModel? BuildChipGroup(
        string categorySlug,
        IReadOnlyList<string> chipValues)
    {
        var links = (chipValues.Count > 0
                ? chipValues.Concat(LaptopChipFallbacks)
                : LaptopChipFallbacks)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetChipSortRank)
            .ThenBy(chip => chip, StringComparer.CurrentCultureIgnoreCase)
            .Take(16)
            .Select(chip => new SiteCategoryMenuLinkViewModel
            {
                Label = chip,
                Url = BuildCatalogUrl(categorySlug, ("f_chip", Slugify(chip)))
            })
            .ToList();

        return links.Count == 0
            ? null
            : new SiteCategoryMenuGroupViewModel
            {
                Title = "Dòng chip",
                Links = links
            };
    }

    private static SiteCategoryMenuGroupViewModel? BuildFilterGroup(
        string title,
        string categorySlug,
        string queryKey,
        IReadOnlyList<FilterLinkDefinition> definitions)
    {
        var links = definitions
            .Select(definition => new SiteCategoryMenuLinkViewModel
            {
                Label = definition.Label,
                Url = BuildCatalogUrl(categorySlug, ($"f_{queryKey}", definition.Value))
            })
            .ToList();

        return links.Count == 0
            ? null
            : new SiteCategoryMenuGroupViewModel
            {
                Title = title,
                Links = links
            };
    }

    private static SiteCategoryMenuLinkViewModel CreateCategoryLink(
        CategoryRecord category,
        bool includeImage)
    {
        return new SiteCategoryMenuLinkViewModel
        {
            Label = category.Name,
            Url = BuildCatalogUrl(category.Slug),
            ImageUrl = includeImage ? NormalizeOptionalImageUrl(category.ImagePath) : null,
            ImageAlt = category.Name
        };
    }

    private static IReadOnlyList<CategoryRecord> ResolveExplicitRow(
        CategoryRecord currentCategory,
        IReadOnlyList<CategoryRecord> categories,
        ISet<string> consumedSlugs)
    {
        var rowDefinition = ExplicitCategoryRows.FirstOrDefault(definition =>
            definition.CategoryAliases.Any(aliases => aliases.Any(alias =>
                MatchesAny(currentCategory, alias))));

        if (rowDefinition is null)
        {
            return [];
        }

        var rowCategories = rowDefinition.CategoryAliases
            .Select(aliases => categories.FirstOrDefault(category =>
                !consumedSlugs.Contains(category.Slug)
                && aliases.Any(alias => MatchesAny(category, alias))))
            .Where(category => category is not null)
            .Select(category => category!)
            .ToList();

        return rowCategories.Count > 1 ? rowCategories : [];
    }

    private static IReadOnlyList<CategoryRecord> GetDirectChildren(
        CategoryRecord parent,
        IReadOnlyList<CategoryRecord> categories)
    {
        return categories
            .Where(category => category.ParentId == parent.Id)
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .ToList();
    }

    private static HashSet<long> GetCategoryTreeIds(
        IReadOnlyList<CategoryRecord> roots,
        IReadOnlyList<CategoryRecord> categories)
    {
        var result = roots.Select(category => category.Id).ToHashSet();
        var queue = new Queue<long>(result);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var child in categories.Where(category => category.ParentId == parentId))
            {
                if (result.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    private static CategoryRecord? ResolveAudioCategory(
        IReadOnlyList<CategoryRecord> rowCategories,
        IReadOnlyList<CategoryRecord> categories,
        string slug)
    {
        var rootIds = rowCategories.Select(category => category.Id).ToHashSet();
        return rowCategories
            .Concat(categories.Where(category =>
                category.ParentId is long parentId && rootIds.Contains(parentId)))
            .FirstOrDefault(category =>
                string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveAudioGroupClass(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "tai-nghe", "headphone", "earphone"))
        {
            return "site-category-mega-group--audio-headphones";
        }

        if (ContainsAny(text, "loa", "speaker"))
        {
            return "site-category-mega-group--audio-speakers";
        }

        if (ContainsAny(text, "mic", "microphone"))
        {
            return "site-category-mega-group--audio-microphones";
        }

        return "site-category-mega-group--audio-other";
    }

    private static string ResolveHomeElectronicsGroupClass(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "tivi", "tv"))
        {
            return "site-category-mega-group--electronics-tv";
        }

        if (ContainsAny(text, "dien-lanh", "may-lanh", "tu-lanh", "tu-dong", "may-giat"))
        {
            return "site-category-mega-group--electronics-cooling";
        }

        if (ContainsAny(text, "giai-tri", "entertainment", "may-chieu", "loa-thanh"))
        {
            return "site-category-mega-group--electronics-entertainment";
        }

        return "site-category-mega-group--electronics-categories";
    }

    private static bool IsTvCategory(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";
        return ContainsAny(text, "tivi", "tv");
    }

    private static string ResolveChildGroupTitle(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "dien-thoai", "phone", "smartphone"))
        {
            return "Điện thoại HOT ⚡";
        }

        if (ContainsAny(text, "tablet", "may-tinh-bang"))
        {
            return "Máy tính bảng HOT ⚡";
        }

        if (ContainsAny(text, "laptop"))
        {
            return "Nhu cầu sử dụng";
        }

        if (ContainsAny(text, "am-thanh", "audio", "tai-nghe"))
        {
            return "Chọn loại tai nghe";
        }

        if (ContainsAny(text, "mic"))
        {
            return "Mic";
        }

        if (ContainsAny(text, "dong-ho", "watch", "smartwatch"))
        {
            return "Loại đồng hồ";
        }

        if (ContainsAny(text, "camera", "may-anh"))
        {
            return "Camera";
        }

        if (ContainsAny(text, "pc", "desktop", "may-tinh"))
        {
            return "Loại PC";
        }

        if (ContainsAny(text, "man-hinh", "monitor"))
        {
            return "Chọn màn hình theo nhu cầu";
        }

        if (ContainsAny(text, "may-in", "printer"))
        {
            return "Thiết bị văn phòng";
        }

        return category.Name;
    }

    private static string ResolveBrandGroupTitle(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "pc", "desktop", "may-tinh-de-ban"))
        {
            return "Thương hiệu PC";
        }

        if (ContainsAny(text, "man-hinh", "monitor"))
        {
            return "Thương hiệu màn hình";
        }

        if (ContainsAny(text, "may-in", "printer"))
        {
            return "Thương hiệu máy in";
        }

        return $"Thương hiệu {category.Name.ToLowerInvariant()}";
    }

    private static string ResolveHomeElectronicsBrandGroupTitle(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        return ContainsAny(text, "tivi", "tv")
            ? "Thương hiệu Tivi"
            : "Thương hiệu điện máy";
    }

    private static string ResolvePriceGroupTitle(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "pc", "desktop", "may-tinh-de-ban"))
        {
            return "Mức giá PC";
        }

        if (ContainsAny(text, "man-hinh", "monitor"))
        {
            return "Mức giá màn hình";
        }

        if (ContainsAny(text, "may-in", "printer"))
        {
            return "Mức giá máy in";
        }

        return "Mức giá";
    }

    private static string ResolveHomeElectronicsPriceGroupTitle(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        return ContainsAny(text, "tivi", "tv")
            ? "Mức giá Tivi"
            : "Mức giá điện máy";
    }

    private static IReadOnlyList<FilterLinkDefinition> ResolveComputerPriceRanges(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        return ContainsAny(text, "pc", "desktop", "may-tinh-de-ban")
            ? ComputerPriceRanges
            : PeripheralPriceRanges;
    }

    private static int GetMenuSortRank(CategoryRecord category)
    {
        var text = $"{category.Slug} {category.Name}";

        if (ContainsAny(text, "dien-thoai", "phone", "smartphone", "tablet", "may-tinh-bang"))
        {
            return 10;
        }

        if (ContainsAny(text, "laptop"))
        {
            return 20;
        }

        if (ContainsAny(text, "am-thanh", "audio", "mic", "tai-nghe", "loa"))
        {
            return 30;
        }

        if (ContainsAny(text, "dong-ho", "watch", "smartwatch", "camera", "may-anh"))
        {
            return 40;
        }

        if (ContainsAny(text, "do-gia-dung", "gia-dung", "lam-dep", "suc-khoe", "appliance"))
        {
            return 50;
        }

        if (ContainsAny(text, "phu-kien", "accessory", "cable", "cap-sac"))
        {
            return 60;
        }

        if (ContainsAny(text, "pc", "desktop", "may-tinh", "man-hinh", "monitor", "may-in", "printer"))
        {
            return 70;
        }

        if (ContainsAny(text, "tivi", "tv", "dien-may", "home-electronics", "dien-lanh"))
        {
            return 80;
        }

        if (ContainsAny(text, "thu-cu", "doi-moi", "trade"))
        {
            return 90;
        }

        if (ContainsAny(text, "hang-cu", "used"))
        {
            return 100;
        }

        if (ContainsAny(text, "khuyen-mai", "deal", "sale", "discount"))
        {
            return 110;
        }

        if (ContainsAny(text, "tin-cong-nghe", "news", "blog"))
        {
            return 120;
        }

        return 1000;
    }

    private static string BuildItemId(
        int rowNumber,
        IReadOnlyList<CategoryRecord> categories)
    {
        var firstSlug = categories[0].Slug;
        return $"site-cat-{rowNumber}-{firstSlug}";
    }

    private static string BuildCatalogUrl(
        string slug,
        params (string Key, string Value)[] queryPairs)
    {
        var query = new List<string>
        {
            $"cat={Uri.EscapeDataString(slug)}"
        };

        foreach (var pair in queryPairs)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key)
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                query.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}");
            }
        }

        return $"/catalog?{string.Join('&', query)}";
    }

    private static bool IsHighlighted(IReadOnlyList<CategoryRecord> categories)
    {
        return categories.Any(category => ContainsAny(category.Slug, "khuyen-mai", "deal", "sale", "discount"));
    }

    private static string ResolveIcon(IReadOnlyList<CategoryRecord> categories)
    {
        var firstCategory = categories[0];
        var text = $"{firstCategory.Slug} {firstCategory.Name}";

        if (ContainsAny(text, "dien-thoai", "tablet", "phone", "smartphone"))
        {
            return "phone";
        }

        if (ContainsAny(text, "laptop"))
        {
            return "laptop";
        }

        if (ContainsAny(text, "am-thanh", "audio", "mic", "tai-nghe", "loa"))
        {
            return "audio";
        }

        if (ContainsAny(text, "dong-ho", "watch", "camera"))
        {
            return "watch";
        }

        if (ContainsAny(text, "do-gia-dung", "gia-dung", "lam-dep", "suc-khoe", "appliance"))
        {
            return "home";
        }

        if (ContainsAny(text, "phu-kien", "accessory", "cable", "cap-sac"))
        {
            return "cable";
        }

        if (ContainsAny(text, "pc", "desktop", "may-tinh", "man-hinh", "may-in", "monitor", "printer"))
        {
            return "desktop";
        }

        if (ContainsAny(text, "tivi", "tv", "dien-may"))
        {
            return "tv";
        }

        if (ContainsAny(text, "thu-cu", "doi-moi", "trade"))
        {
            return "swap";
        }

        if (ContainsAny(text, "hang-cu", "used"))
        {
            return "history";
        }

        if (ContainsAny(text, "khuyen-mai", "deal", "sale", "discount"))
        {
            return "discount";
        }

        if (ContainsAny(text, "tin-cong-nghe", "news", "blog"))
        {
            return "news";
        }

        return "phone";
    }

    private static bool HasCategory(IReadOnlyList<CategoryRecord> categories, params string[] aliases)
    {
        return categories.Any(category => aliases.Any(alias => MatchesAny(category, alias)));
    }

    private static bool MatchesAny(CategoryRecord category, params string[] keywords)
    {
        var value = $"{category.Slug} {category.Name}";
        return ContainsAny(value, keywords);
    }

    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLaptopChipSpecification(ProductSpecificationRecord specification)
    {
        var text = $"{specification.Key} {specification.Name}";
        return ContainsAny(text, "chip", "cpu", "processor", "vi xử lý", "bo vi xử lý");
    }

    private static IEnumerable<string> ResolveChipLabels(string value)
    {
        var normalized = value.ToLowerInvariant();
        var labels = new List<string>();

        AddWhenContains("Laptop Core i3", "core i3");
        AddWhenContains("Laptop Core i5", "core i5");
        AddWhenContains("Laptop Core i7", "core i7");
        AddWhenContains("Laptop Core i9", "core i9");
        AddWhenContains("Intel Core Ultra", "core ultra");
        AddWhenContains("Apple M1 Series", "m1");
        AddWhenContains("Apple M2 Series", "m2");
        AddWhenContains("Apple M3 Series", "m3");
        AddWhenContains("Apple M4 Series", "m4");
        AddWhenContains("Apple M5 Series", "m5");
        AddWhenContains("AMD Ryzen", "ryzen");

        return labels;

        void AddWhenContains(string label, string keyword)
        {
            if (normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                labels.Add(label);
            }
        }
    }

    private static int GetChipSortRank(string value)
    {
        var fallbackIndex = Array.FindIndex(
            LaptopChipFallbacks,
            chip => string.Equals(chip, value, StringComparison.OrdinalIgnoreCase));

        return fallbackIndex >= 0
            ? fallbackIndex
            : 100;
    }

    private static void AddIfNotNull(
        ICollection<SiteCategoryMenuGroupViewModel> groups,
        SiteCategoryMenuGroupViewModel? group)
    {
        if (group is not null)
        {
            groups.Add(group);
        }
    }

    private static SiteCategoryMenuGroupViewModel? WithGroupClass(
        SiteCategoryMenuGroupViewModel? group,
        string cssClass)
    {
        if (group is null)
        {
            return null;
        }

        return new SiteCategoryMenuGroupViewModel
        {
            Title = group.Title,
            Links = group.Links,
            CssClass = string.IsNullOrWhiteSpace(group.CssClass)
                ? cssClass
                : $"{group.CssClass} {cssClass}"
        };
    }

    private static string GetBrandUrlValue(string? slug, string name)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? Slugify(name)
            : slug;
    }

    private static string? NormalizeOptionalImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith('/'))
        {
            return imagePath;
        }

        return "/" + imagePath.TrimStart('/');
    }

    private static string Slugify(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(' ', '-');
    }

    private sealed record CategoryRecord(
        long Id,
        long? ParentId,
        string Name,
        string Slug,
        string? ImagePath,
        int Position);

    private sealed record BrandCategoryRecord(
        long CategoryId,
        long BrandId,
        string Name,
        string Slug,
        string? ImagePath);

    private sealed record ActiveBrandRecord(
        long BrandId,
        string Name,
        string Slug,
        string? ImagePath);

    private sealed record NestedCategoryGroupRecord(
        CategoryRecord LevelTwoCategory,
        SiteCategoryMenuGroupViewModel Group);

    private sealed record ProductSpecificationRecord(
        string Key,
        string Name,
        string Value);

    private sealed record FilterLinkDefinition(
        string Label,
        string Value);

    private sealed record CategoryLinkGroupDefinition(string[][] CategoryAliases);
}
