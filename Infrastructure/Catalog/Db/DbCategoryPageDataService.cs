using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Infrastructure.Home.Db;
using e_commerce_web_customer.Models.Constants;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.ViewModels.Catalog;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.RegularExpressions;

namespace e_commerce_web_customer.Infrastructure.Catalog.Db;

public sealed class DbCategoryPageDataService(
    EcommerceDbContext dbContext,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate) : ICategoryPageDataService
{
    private const string FallbackImageUrl = "/images/logo-techstore-icon.svg";
    private const int InitialProductCount = 20;
    private const int SectionProductLimit = 20;
    private const int NewArrivalWindowDays = 60;
    private const int MaxDynamicFilterGroups = 12;
    private const int MaxDynamicFilterOptions = 24;
    private const string AttributeFilterPrefix = "attribute-";
    private const string SpecificationFilterPrefix = "specification-";
    private const string ScreenSizeFilterKey = "screen-size";

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

    private static readonly IReadOnlyDictionary<string, string> CategoryAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["phone"] = "dien-thoai",
            ["mobile"] = "dien-thoai",
            ["audio"] = "am-thanh",
            ["speaker"] = "am-thanh",
            ["microphone"] = "am-thanh",
            ["watch"] = "dong-ho",
            ["smartwatch"] = "dong-ho",
            ["monitor"] = "man-hinh",
            ["desktop"] = "pc",
            ["computer-accessories"] = "may-in",
            ["accessories"] = "phu-kien",
            ["appliances"] = "do-gia-dung",
            ["home-electronics"] = "dien-may",
            ["trade-in"] = "thu-cu-doi-moi",
            ["used"] = "hang-cu",
            ["deals"] = "khuyen-mai",
            ["tech"] = "tin-cong-nghe"
        };

    private static readonly IReadOnlyList<PhoneFilterGroupDefinition> PhoneFilterGroups =
    [
        new(
            "price",
            "Xem theo giá",
            "price",
            [
                new("under-10", "Dưới 10 triệu"),
                new("10-20", "Từ 10 - 20 triệu"),
                new("20-30", "Từ 20 - 30 triệu"),
                new("over-30", "Trên 30 triệu")
            ]),
        new(
            "need",
            "Nhu cầu sử dụng",
            null,
            [
                new("gaming", "Chơi game"),
                new("photography", "Chụp ảnh, quay phim"),
                new("long-battery", "Pin lâu"),
                new("ai", "Điện thoại AI"),
                new("basic", "Nhu cầu cơ bản")
            ]),
        new(
            "chipset",
            "Chip xử lí",
            null,
            [
                new("snapdragon", "Snapdragon"),
                new("apple-a", "Apple A"),
                new("mediatek-dimensity", "Mediatek Dimensity"),
                new("mediatek-helio", "Mediatek Helio"),
                new("exynos", "Exynos"),
                new("unisoc", "Unisoc")
            ]),
        new(
            "phone-type",
            "Loại điện thoại",
            null,
            [
                new("ios", "iPhone (iOS)"),
                new("android", "Android"),
                new("feature-phone", "Điện thoại phổ thông")
            ]),
        new(
            "ram",
            "Dung lượng RAM",
            null,
            [
                new("under-3", "Dưới 3 GB"),
                new("3", "3 GB"),
                new("4", "4 GB"),
                new("6", "6 GB"),
                new("8", "8 GB"),
                new("12", "12 GB"),
                new("16", "16 GB")
            ]),
        new(
            "storage",
            "Bộ nhớ trong",
            null,
            [
                new("under-64", "Dưới 64 GB"),
                new("64", "64 GB"),
                new("128", "128 GB"),
                new("256", "256 GB"),
                new("512", "512 GB"),
                new("1024", "1 TB"),
                new("2048", "2 TB")
            ]),
        new(
            "special",
            "Tính năng đặc biệt",
            null,
            [
                new("wireless-charging", "Sạc không dây"),
                new("fingerprint", "Bảo mật vân tay"),
                new("face-unlock", "Nhận diện khuôn mặt"),
                new("water-resistant", "Kháng nước, kháng bụi"),
                new("5g", "Hỗ trợ 5G"),
                new("phone-ai", "Điện thoại AI"),
                new("stylus", "Đi kèm bút cảm ứng")
            ]),
        new(
            "camera",
            "Tính năng camera",
            null,
            [
                new("portrait", "Chụp xóa phông"),
                new("ultrawide", "Chụp góc rộng"),
                new("video-4k", "Quay video 4K"),
                new("telephoto", "Chụp Zoom xa"),
                new("macro", "Chụp macro"),
                new("stabilization", "Chống rung"),
                new("video-8k", "Quay video 8K"),
                new("camera-ai", "Camera AI"),
                new("motion", "Chụp ảnh chuyển động"),
                new("night", "Chụp đêm")
            ]),
        new(
            "refresh-rate",
            "Tần số quét",
            null,
            [
                new("60", "60Hz"),
                new("120", "120Hz"),
                new("90", "90Hz"),
                new("144-plus", "Từ 144Hz trở lên")
            ]),
        new(
            "screen-type",
            "Kiểu màn hình",
            null,
            [
                new("pill", "Tai thỏ"),
                new("borderless", "Tràn viền (Không khiếm khuyết)"),
                new("foldable", "Màn hình gập"),
                new("waterdrop", "Giọt nước"),
                new("hole-punch", "Đục lỗ (Nốt ruồi)"),
                new("dynamic-island", "Dynamic Island")
            ]),
        new(
            "nfc",
            "Công nghệ NFC",
            null,
            [
                new("yes", "Có"),
                new("no", "Không")
            ]),
        new(
            "utility",
            "Tiện ích khác",
            null,
            [
                new("large-battery", "Pin khủng"),
                new("face-unlock", "Nhận diện khuôn mặt"),
                new("reading-mode", "Chế độ đọc sách"),
                new("kids-mode", "Chế độ cho trẻ em")
            ]),
        new(
            "network",
            "Hỗ trợ mạng",
            null,
            [
                new("5g", "5G"),
                new("4g", "4G")
            ])
    ];

    private static readonly IReadOnlyList<PhoneFilterOptionDefinition> TvScreenSizeFilterOptions =
    [
        new("32", "32 inch"),
        new("43", "43 inch"),
        new("55", "55 inch"),
        new("60", "60 inch"),
        new("65", "65 inch"),
        new("75", "75 inch")
    ];

    public Task<CategoryPageViewModel?> CreateCategoryPageAsync(
        CategoryPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(request);

        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => dbQueryGate.RunAsync(
                () => CreateCategoryPageUncachedAsync(request, CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
    }

    private async Task<CategoryPageViewModel?> CreateCategoryPageUncachedAsync(
        CategoryPageRequest request,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .AsSplitQuery()
            .Include(category => category.CategoryVariantAttributes)
                .ThenInclude(categoryAttribute => categoryAttribute.Attribute)
            .Include(category => category.CategorySpecifications)
                .ThenInclude(categorySpecification => categorySpecification.Specification)
            .Where(category => category.IsActive)
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .ToListAsync(cancellationToken);

        var requestedSlug = NormalizeCategorySlug(request.Slug);
        var category = ResolveRequestedCategory(categories, requestedSlug);
        if (category is null)
        {
            return null;
        }

        var categoryIds = GetSubtreeCategoryIds(category.Id, categories);
        var directChildren = GetDirectChildren(category.Id, categories);
        var usesSectionedLayout = directChildren.Any(child =>
            categories.Any(grandchild => grandchild.ParentId == child.Id));
        var hidesGlobalFilter = usesSectionedLayout && HidesGlobalFilter(category);
        var allVariants = await GetVariantsAsync(
            categoryIds,
            includeProductSpecifications: !hidesGlobalFilter || HasSpecificationFilter(request),
            cancellationToken);
        var brands = BuildBrands(category, allVariants, request);
        if (IsApplianceRoot(category)
            && brands.Count == 0)
        {
            brands = await GetFallbackApplianceBrandsAsync(category.Slug, cancellationToken);
        }

        var brandFilteredVariants = ApplyBrandFilter(allVariants, request.Brand);
        var filteredVariants = ApplyCatalogFilters(brandFilteredVariants, request);
        var allProductCards = BuildProductCards(filteredVariants, request.Sort);
        var totalProductCount = allProductCards.Count;
        var productCards = GetProductPage(allProductCards, request.Page);

        return usesSectionedLayout
            ? BuildSectionedPage(
                category,
                categories,
                allVariants,
                brandFilteredVariants,
                filteredVariants,
                brands,
                productCards,
                totalProductCount,
                request)
            : BuildFilterListingPage(
                category,
                categories,
                allVariants,
                brandFilteredVariants,
                filteredVariants,
                brands,
                productCards,
                totalProductCount,
                request);
    }

    private async Task<List<ProductVariant>> GetVariantsAsync(
        IReadOnlyCollection<long> categoryIds,
        bool includeProductSpecifications,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductVariant> query = dbContext.ProductVariants
            .AsNoTracking()
            .AsSplitQuery()
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Brand)
            .Include(variant => variant.ProductVariantImages
                .OrderBy(image => image.Position)
                .ThenBy(image => image.Id)
                .Take(1))
            .Include(variant => variant.VariantAttributes)
                .ThenInclude(variantAttribute => variantAttribute.AttributeOption)
                    .ThenInclude(option => option!.Attribute);

        if (includeProductSpecifications)
        {
            query = query
                .Include(variant => variant.Product)
                    .ThenInclude(product => product!.ProductSpecifications)
                        .ThenInclude(productSpecification => productSpecification.Specification);
        }

        return await query
            .Where(variant => variant.IsActive)
            .Where(variant => variant.Product != null && variant.Product.IsActive)
            .Where(variant => variant.Product != null
                && categoryIds.Contains(variant.Product.CategoryId))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CategoryBrandViewModel>> GetFallbackApplianceBrandsAsync(
        string categorySlug,
        CancellationToken cancellationToken)
    {
        var allowedSlugs = ApplianceBrandFallbackSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var brands = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive && allowedSlugs.Contains(brand.Slug))
            .Select(brand => new
            {
                brand.Name,
                brand.Slug,
                brand.ImagePath
            })
            .ToListAsync(cancellationToken);

        return brands
            .OrderBy(brand => Array.FindIndex(
                ApplianceBrandFallbackSlugs,
                slug => string.Equals(slug, brand.Slug, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(brand => brand.Name)
            .Select(brand => new CategoryBrandViewModel
            {
                Id = string.IsNullOrWhiteSpace(brand.Slug)
                    ? Slugify(brand.Name)
                    : brand.Slug,
                Label = brand.Name,
                Url = BuildCatalogUrl(
                    categorySlug,
                    string.IsNullOrWhiteSpace(brand.Slug) ? brand.Name : brand.Slug),
                ImageUrl = NormalizeOptionalImageUrl(brand.ImagePath),
                ImageAlt = $"Logo {brand.Name}"
            })
            .ToList();
    }

    private static CategoryPageViewModel BuildFilterListingPage(
        Category category,
        IReadOnlyList<Category> categories,
        IReadOnlyList<ProductVariant> allVariants,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyList<ProductVariant> filteredVariants,
        IReadOnlyList<CategoryBrandViewModel> brands,
        IReadOnlyList<ProductCardViewModel> productCards,
        int totalProductCount,
        CategoryPageRequest request)
    {
        var directChildren = GetDirectChildren(category.Id, categories);

        return new CategoryPageViewModel
        {
            Slug = category.Slug,
            Title = category.Name,
            MetaDescription = BuildMetaDescription(category),
            LayoutMode = CategoryPageLayoutMode.FilterListing,
            Breadcrumbs = BuildBreadcrumbs(category, categories),
            PromotionBanners = [],
            Brands = brands,
            QuickLinks = BuildCategoryQuickLinks(directChildren, allVariants),
            HotSale = BuildHotSale(filteredVariants, request.Sort),
            Filter = BuildFilter(
                category,
                categories,
                filterSourceVariants,
                totalProductCount,
                request),
            Products = productCards,
            TotalProductCount = totalProductCount,
            CurrentPage = NormalizePage(request.Page),
            HasMoreProducts = NormalizePage(request.Page) * InitialProductCount < totalProductCount,
            InitialProductCount = InitialProductCount,
            SeoContent = BuildSeoContent(category),
            QuestionAnswer = BuildQuestionAnswer(category)
        };
    }

    private static CategoryPageViewModel BuildSectionedPage(
        Category category,
        IReadOnlyList<Category> categories,
        IReadOnlyList<ProductVariant> allVariants,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyList<ProductVariant> filteredVariants,
        IReadOnlyList<CategoryBrandViewModel> brands,
        IReadOnlyList<ProductCardViewModel> productCards,
        int totalProductCount,
        CategoryPageRequest request)
    {
        var directChildren = GetDirectChildren(category.Id, categories);
        var isAccessoryDirectory = string.Equals(
            category.Slug,
            "phu-kien",
            StringComparison.OrdinalIgnoreCase);
        var isAudioRoot = IsAudioRoot(category);
        var isApplianceRoot = IsApplianceRoot(category);
        var hidesGlobalFilter = isAccessoryDirectory || isApplianceRoot || isAudioRoot;
        var sections = directChildren
            .Select(child => BuildProductSection(
                child,
                categories,
                filteredVariants,
                request))
            .ToList();

        return new CategoryPageViewModel
        {
            Slug = category.Slug,
            Title = category.Name,
            MetaDescription = BuildMetaDescription(category),
            LayoutMode = CategoryPageLayoutMode.Sectioned,
            Breadcrumbs = BuildBreadcrumbs(category, categories),
            PromotionBanners = [],
            Brands = isAccessoryDirectory ? [] : brands,
            QuickLinks = isAccessoryDirectory
                ? BuildAccessoryDirectoryLinks(category, categories)
                : isApplianceRoot
                    ? []
                : BuildCategoryQuickLinks(directChildren, allVariants),
            HotSale = BuildHotSale(filteredVariants, request.Sort),
            Filter = hidesGlobalFilter
                ? BuildEmptyFilter(category, totalProductCount, request)
                : BuildFilter(
                    category,
                    categories,
                    filterSourceVariants,
                    totalProductCount,
                    request),
            Products = productCards,
            TotalProductCount = totalProductCount,
            CurrentPage = NormalizePage(request.Page),
            HasMoreProducts = NormalizePage(request.Page) * InitialProductCount < totalProductCount,
            InitialProductCount = InitialProductCount,
            SectionTabs = sections.Select((section, index) => new CategorySectionNavigationItemViewModel
            {
                Id = section.Id.Replace("section-", string.Empty, StringComparison.Ordinal),
                Label = section.Title,
                Url = $"#{section.Id}",
                IsActive = index == 0
            }).ToList(),
            ProductSections = sections,
            IsAccessoryDirectory = isAccessoryDirectory,
            SeoContent = BuildSeoContent(category),
            QuestionAnswer = BuildQuestionAnswer(category)
        };
    }

    private static IReadOnlyList<CategoryQuickLinkViewModel> BuildAccessoryDirectoryLinks(
        Category rootCategory,
        IReadOnlyList<Category> categories)
    {
        var secondLevelCategories = GetDirectChildren(rootCategory.Id, categories);

        return secondLevelCategories
            .SelectMany(category => GetDirectChildren(category.Id, categories))
            .Take(24)
            .Select(category => new CategoryQuickLinkViewModel
            {
                Label = category.Name,
                Url = BuildCatalogUrl(category.Slug),
                ImageUrl = NormalizeImageUrl(category.ImagePath),
                ImageAlt = category.Name
            })
            .ToList();
    }

    private static bool IsApplianceRoot(Category category)
    {
        return string.Equals(category.Slug, "do-gia-dung", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAudioRoot(Category category)
    {
        return string.Equals(category.Slug, "am-thanh", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HidesGlobalFilter(Category category)
    {
        return IsApplianceRoot(category)
            || IsAudioRoot(category)
            || string.Equals(category.Slug, "phu-kien", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSpecificationFilter(CategoryPageRequest request)
    {
        return request.Filters?.Keys.Any(key =>
            key.StartsWith(SpecificationFilterPrefix, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static CategoryProductSectionViewModel BuildProductSection(
        Category sectionCategory,
        IReadOnlyList<Category> categories,
        IReadOnlyList<ProductVariant> filteredVariants,
        CategoryPageRequest request)
    {
        var sectionCategoryIds = GetSubtreeCategoryIds(sectionCategory.Id, categories);
        var sectionVariants = filteredVariants
            .Where(variant => variant.Product is not null
                && sectionCategoryIds.Contains(variant.Product.CategoryId))
            .ToList();
        var subcategories = GetDirectChildren(sectionCategory.Id, categories);
        var sectionProductCards = BuildProductCards(sectionVariants, request.Sort);

        return new CategoryProductSectionViewModel
        {
            Id = $"section-{sectionCategory.Slug}",
            Title = sectionCategory.Name,
            Description = sectionCategory.Description,
            ViewAllUrl = BuildCatalogUrl(sectionCategory.Slug, request.Brand, request.Sort),
            VisibleProductLimit = SectionProductLimit,
            Banner = BuildAccessorySectionBanner(sectionCategory),
            Subcategories = subcategories
                .Select(subcategory => BuildSectionPill(subcategory, sectionVariants))
                .ToList(),
            SortOptions = BuildSortOptions(sectionCategory.Slug, request.Brand, request.Sort),
            Products = sectionProductCards.Take(SectionProductLimit).ToList(),
            TotalProductCount = sectionProductCards.Count
        };
    }

    private static CategorySectionBannerViewModel? BuildAccessorySectionBanner(
        Category category)
    {
        var imageUrl = category.Slug switch
        {
            "phu-kien-di-dong" => "/images/banner_phu_kien/phu-kien-di-dong.webp",
            "phu-kien-laptop" => "/images/banner_phu_kien/phu-kien-laptop.webp",
            "thiet-bi-mang" => "/images/banner_phu_kien/thiet-bi-mang.webp",
            "thiet-bi-luu-tru" => "/images/banner_phu_kien/thiet-bi-luu-tru.webp",
            "camera" => "/images/banner_phu_kien/camera.webp",
            "thiet-bi-gia-dinh" => "/images/banner_gia_dung/thiet-bi-gia-dinh.webp",
            "gia-dung-nha-bep" => "/images/banner_gia_dung/gia-dung-nha-bep.webp",
            "suc-khoe-lam-dep" => "/images/banner_gia_dung/suc-khoe-lam-dep.webp",
            "tai-nghe" => "/images/banner_am_thanh/tai-nghe.webp",
            "loa" => "/images/banner_am_thanh/loa.webp",
            "phu-kien-am-thanh" => "/images/banner_am_thanh/phu-kien-am-thanh.webp",
            _ => null
        };

        if (imageUrl is null)
        {
            return null;
        }

        return new CategorySectionBannerViewModel
        {
            Title = category.Name,
            Subtitle = string.Empty,
            ImageUrl = imageUrl,
            ImageAlt = $"Ưu đãi {category.Name.ToLowerInvariant()}",
            IsFullWidthImage = true
        };
    }

    private static CategorySectionPillViewModel BuildSectionPill(
        Category category,
        IReadOnlyList<ProductVariant> sectionVariants)
    {
        var imageUrl = NormalizeImageUrl(category.ImagePath);
        if (imageUrl == FallbackImageUrl)
        {
            imageUrl = sectionVariants
                .Where(variant => variant.Product?.CategoryId == category.Id)
                .Select(GetVariantImageUrl)
                .FirstOrDefault(url => url != FallbackImageUrl)
                ?? FallbackImageUrl;
        }

        return new CategorySectionPillViewModel
        {
            Label = category.Name,
            Url = BuildCatalogUrl(category.Slug),
            ImageUrl = imageUrl,
            ImageAlt = category.Name
        };
    }

    private static IReadOnlyList<CategoryBrandViewModel> BuildBrands(
        Category category,
        IReadOnlyList<ProductVariant> variants,
        CategoryPageRequest request)
    {
        return variants
            .Where(variant => variant.Product?.Brand is { IsActive: true })
            .Select(variant => variant.Product!.Brand!)
            .GroupBy(brand => brand.Id)
            .Select(group => group.First())
            .OrderBy(brand => brand.Name)
            .Select(brand => new CategoryBrandViewModel
            {
                Id = string.IsNullOrWhiteSpace(brand.Slug)
                    ? Slugify(brand.Name)
                    : brand.Slug,
                Label = brand.Name,
                Url = BuildCatalogStateUrl(
                    category.Slug,
                    string.IsNullOrWhiteSpace(brand.Slug) ? brand.Name : brand.Slug,
                    request.Sort,
                    request.Filters,
                    request.InStockOnly,
                    request.NewArrivalsOnly),
                ImageUrl = NormalizeOptionalImageUrl(brand.ImagePath),
                ImageAlt = $"Logo {brand.Name}"
            })
            .ToList();
    }

    private static IReadOnlyList<ProductVariant> ApplyBrandFilter(
        IReadOnlyList<ProductVariant> variants,
        string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return variants;
        }

        var normalizedBrand = brand.Trim();
        return variants
            .Where(variant => variant.Product?.Brand is not null
                && (string.Equals(
                        variant.Product.Brand.Slug,
                        normalizedBrand,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        variant.Product.Brand.Name,
                        normalizedBrand,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static IReadOnlyList<ProductVariant> ApplyCatalogFilters(
        IReadOnlyList<ProductVariant> variants,
        CategoryPageRequest request)
    {
        IEnumerable<ProductVariant> filtered = variants;

        if (request.InStockOnly)
        {
            filtered = filtered.Where(variant => variant.Quantity > 0);
        }

        if (request.NewArrivalsOnly)
        {
            var createdAfter = DateTime.UtcNow.AddDays(-NewArrivalWindowDays);
            filtered = filtered.Where(variant =>
                variant.Product is not null
                && variant.Product.CreatedAt >= createdAfter);
        }

        if (request.Filters is null || request.Filters.Count == 0)
        {
            return filtered.ToList();
        }

        foreach (var filter in request.Filters)
        {
            var selectedValues = filter.Value
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (selectedValues.Count == 0)
            {
                continue;
            }

            filtered = filtered.Where(variant =>
                selectedValues.Any(value =>
                    MatchesCatalogFilter(variant, filter.Key, value)));
        }

        return filtered.ToList();
    }

    private static bool MatchesCatalogFilter(
        ProductVariant variant,
        string groupKey,
        string optionValue)
    {
        if (TryParseFilterEntityId(groupKey, AttributeFilterPrefix, out var attributeId))
        {
            return variant.VariantAttributes.Any(item =>
                item.AttributeOption?.AttributeId == attributeId
                && string.Equals(
                    item.AttributeOptionId.ToString(CultureInfo.InvariantCulture),
                    optionValue,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (TryParseFilterEntityId(groupKey, SpecificationFilterPrefix, out var specificationId))
        {
            return variant.Product?.ProductSpecifications.Any(item =>
                item.SpecificationId == specificationId
                && string.Equals(
                    item.Value.Trim(),
                    optionValue.Trim(),
                    StringComparison.OrdinalIgnoreCase)) == true;
        }

        if (string.Equals(groupKey, ScreenSizeFilterKey, StringComparison.OrdinalIgnoreCase))
        {
            return MatchesTvScreenSize(variant, optionValue);
        }

        return MatchesPhoneFilter(variant, groupKey, optionValue);
    }

    private static bool TryParseFilterEntityId(
        string groupKey,
        string prefix,
        out long id)
    {
        id = 0;
        if (!groupKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return long.TryParse(
            groupKey[prefix.Length..],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out id);
    }

    private static bool MatchesPhoneFilter(
        ProductVariant variant,
        string groupKey,
        string optionValue)
    {
        var normalizedGroup = groupKey.Trim().ToLowerInvariant();
        var normalizedOption = optionValue.Trim().ToLowerInvariant();

        return normalizedGroup switch
        {
            "price" => MatchesPrice(variant.Price, normalizedOption),
            "need" => MatchesUsageNeed(variant, normalizedOption),
            "chipset" => MatchesChipset(variant, normalizedOption),
            "phone-type" => MatchesPhoneType(variant, normalizedOption),
            "ram" => MatchesCapacityFilter(
                GetVariantCapacityGb(
                    variant,
                    CatalogAttributeCodes.Ram,
                    CatalogAttributeCodes.RamCapacity),
                normalizedOption),
            "storage" => MatchesCapacityFilter(
                GetVariantCapacityGb(
                    variant,
                    CatalogAttributeCodes.Rom,
                    CatalogAttributeCodes.Storage,
                    CatalogAttributeCodes.InternalStorage),
                normalizedOption),
            "special" => MatchesSpecialFeature(variant, normalizedOption),
            "camera" => MatchesCameraFeature(variant, normalizedOption),
            "refresh-rate" => MatchesRefreshRate(variant, normalizedOption),
            "screen-type" => MatchesScreenType(variant, normalizedOption),
            "nfc" => MatchesNfc(variant, normalizedOption),
            "utility" => MatchesUtility(variant, normalizedOption),
            "network" => MatchesNetwork(variant, normalizedOption),
            _ => true
        };
    }

    private static bool MatchesPrice(decimal price, string option)
    {
        return option switch
        {
            "under-10" => price < 10_000_000m,
            "10-20" => price >= 10_000_000m && price < 20_000_000m,
            "20-30" => price >= 20_000_000m && price < 30_000_000m,
            "over-30" => price >= 30_000_000m,
            _ => false
        };
    }

    private static bool MatchesUsageNeed(ProductVariant variant, string option)
    {
        return option switch
        {
            "gaming" => GetSpecificationNumber(variant, "refresh_rate") >= 120
                || ContainsProductText(variant, ["gaming", "game"]),
            "photography" => HasSpecification(variant, "rear_camera")
                && (ContainsSpecificationText(
                        variant,
                        ["camera_features", "rear_camera", "video_recording"],
                        ["zoom", "goc rong", "xoa phong", "chong rung", "4k", "8k"])
                    || GetSpecificationNumber(variant, "rear_camera") >= 50),
            "long-battery" => GetSpecificationNumber(variant, "battery_capacity") >= 5_000,
            "ai" => ContainsProductText(
                variant,
                ["dien thoai ai", "galaxy ai", "phone ai", "tri tue nhan tao"]),
            "basic" => variant.Price < 5_000_000m
                || MatchesPhoneType(variant, "feature-phone"),
            _ => false
        };
    }

    private static bool MatchesChipset(ProductVariant variant, string option)
    {
        var terms = option switch
        {
            "snapdragon" => new[] { "snapdragon" },
            "apple-a" => new[] { "apple a", "a14", "a15", "a16", "a17", "a18", "a19" },
            "mediatek-dimensity" => new[] { "dimensity" },
            "mediatek-helio" => new[] { "helio" },
            "exynos" => new[] { "exynos" },
            "unisoc" => new[] { "unisoc" },
            _ => []
        };

        return ContainsSpecificationText(variant, ["chipset", "cpu_type"], terms);
    }

    private static bool MatchesPhoneType(ProductVariant variant, string option)
    {
        var operatingSystem = GetSpecificationText(variant, "operating_system");

        return option switch
        {
            "ios" => ContainsAny(operatingSystem, ["ios"])
                || string.Equals(
                    variant.Product?.Brand?.Slug,
                    "apple",
                    StringComparison.OrdinalIgnoreCase),
            "android" => ContainsAny(operatingSystem, ["android"]),
            "feature-phone" => ContainsProductText(
                    variant,
                    ["dien thoai pho thong", "feature phone"])
                || (!string.IsNullOrWhiteSpace(operatingSystem)
                    && !ContainsAny(operatingSystem, ["android", "ios"])),
            _ => false
        };
    }

    private static bool MatchesCapacityFilter(decimal? capacityGb, string option)
    {
        if (!capacityGb.HasValue)
        {
            return false;
        }

        return option switch
        {
            "under-3" => capacityGb.Value < 3,
            "under-64" => capacityGb.Value < 64,
            _ when decimal.TryParse(
                option,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var expected) => capacityGb.Value == expected,
            _ => false
        };
    }

    private static bool MatchesSpecialFeature(ProductVariant variant, string option)
    {
        return option switch
        {
            "wireless-charging" => ContainsSpecificationText(
                variant,
                ["charging_technology"],
                ["sac khong day"]),
            "fingerprint" => ContainsSpecificationText(
                variant,
                ["fingerprint_sensor", "special_features"],
                ["van tay"]),
            "face-unlock" => ContainsSpecificationText(
                variant,
                ["special_features", "features_technology", "sensors"],
                ["nhan dien khuon mat", "face unlock", "face id"]),
            "water-resistant" => HasSpecification(variant, "water_dust_resistance")
                || ContainsSpecificationText(
                    variant,
                    ["special_features"],
                    ["khang nuoc", "khang bui"]),
            "5g" => MatchesNetwork(variant, "5g"),
            "phone-ai" => MatchesUsageNeed(variant, "ai"),
            "stylus" => ContainsProductText(
                variant,
                ["but cam ung", "s pen", "stylus"]),
            _ => false
        };
    }

    private static bool MatchesCameraFeature(ProductVariant variant, string option)
    {
        var terms = option switch
        {
            "portrait" => new[] { "xoa phong", "portrait" },
            "ultrawide" => new[] { "goc sieu rong", "ultrawide" },
            "video-4k" => new[] { "4k", "2160p" },
            "telephoto" => new[] { "tele", "zoom quang hoc", "zoom ky thuat so" },
            "macro" => new[] { "macro", "can canh" },
            "stabilization" => new[] { "chong rung", "ois" },
            "video-8k" => new[] { "8k", "4320p" },
            "camera-ai" => new[] { "camera ai", "ai camera" },
            "motion" => new[] { "chuyen dong", "motion" },
            "night" => new[] { "ban dem", "night mode", "nightography" },
            _ => []
        };

        return ContainsSpecificationText(
            variant,
            ["camera_features", "rear_camera", "front_camera", "video_recording"],
            terms);
    }

    private static bool MatchesRefreshRate(ProductVariant variant, string option)
    {
        var refreshRate = GetSpecificationNumber(variant, "refresh_rate");

        return option switch
        {
            "60" => refreshRate == 60,
            "90" => refreshRate == 90,
            "120" => refreshRate == 120,
            "144-plus" => refreshRate >= 144,
            _ => false
        };
    }

    private static bool MatchesScreenType(ProductVariant variant, string option)
    {
        var terms = option switch
        {
            "pill" => new[] { "tai tho" },
            "borderless" => new[] { "tran vien", "khong khiem khuyet" },
            "foldable" => new[] { "man hinh gap", "gap" },
            "waterdrop" => new[] { "giot nuoc" },
            "hole-punch" => new[] { "duc lo", "not ruoi" },
            "dynamic-island" => new[] { "dynamic island" },
            _ => []
        };

        return ContainsSpecificationText(
            variant,
            ["screen_type", "screen_features"],
            terms);
    }

    private static bool MatchesNfc(ProductVariant variant, string option)
    {
        var nfc = GetSpecificationText(variant, "nfc_technology");

        return option switch
        {
            "yes" => ContainsAny(nfc, ["co", "yes", "nfc"])
                && !ContainsAny(nfc, ["khong"]),
            "no" => ContainsAny(nfc, ["khong", "no"]),
            _ => false
        };
    }

    private static bool MatchesUtility(ProductVariant variant, string option)
    {
        return option switch
        {
            "large-battery" => GetSpecificationNumber(variant, "battery_capacity") >= 5_000,
            "face-unlock" => MatchesSpecialFeature(variant, "face-unlock"),
            "reading-mode" => ContainsProductText(
                variant,
                ["che do doc sach", "reading mode"]),
            "kids-mode" => ContainsProductText(
                variant,
                ["che do cho tre em", "kids mode", "kid mode"]),
            _ => false
        };
    }

    private static bool MatchesNetwork(ProductVariant variant, string option)
    {
        return option switch
        {
            "5g" => ContainsProductText(variant, ["5g"]),
            "4g" => ContainsProductText(variant, ["4g", "lte"])
                && !ContainsProductText(variant, ["5g"]),
            _ => false
        };
    }

    private static decimal? GetVariantCapacityGb(
        ProductVariant variant,
        params string[] attributeCodes)
    {
        var attribute = variant.VariantAttributes.FirstOrDefault(item =>
            attributeCodes.Any(code => string.Equals(
                item.AttributeOption?.Attribute?.Code,
                code,
                StringComparison.OrdinalIgnoreCase)));
        var rawValue = attribute?.AttributeOption?.Label
            ?? attribute?.AttributeOption?.Value;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalized = SearchTextNormalizer.Normalize(rawValue);
        var number = ParseFirstNumber(normalized);
        if (!number.HasValue)
        {
            return null;
        }

        return normalized.Contains("tb", StringComparison.Ordinal)
            ? number.Value * 1024
            : number.Value;
    }

    private static bool HasSpecification(ProductVariant variant, string key)
    {
        return !string.IsNullOrWhiteSpace(GetSpecificationText(variant, key));
    }

    private static decimal GetSpecificationNumber(ProductVariant variant, string key)
    {
        return ParseFirstNumber(GetSpecificationText(variant, key)) ?? 0;
    }

    private static string GetSpecificationText(ProductVariant variant, string key)
    {
        var values = variant.Product?.ProductSpecifications
            .Where(item => string.Equals(
                item.Specification?.Key,
                key,
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return values is null || values.Count == 0
            ? string.Empty
            : SearchTextNormalizer.Normalize(string.Join(' ', values));
    }

    private static bool MatchesTvScreenSize(ProductVariant variant, string option)
    {
        if (!decimal.TryParse(
                option,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var targetSize))
        {
            return false;
        }

        var product = variant.Product;
        var specificationText = product is null
            ? string.Empty
            : string.Join(' ', product.ProductSpecifications.Select(item => item.Value));
        var attributeText = string.Join(
            ' ',
            variant.VariantAttributes.Select(item =>
                item.AttributeOption?.Label ?? item.AttributeOption?.Value));
        var text = SearchTextNormalizer.Normalize(string.Join(
            ' ',
            product?.Name,
            product?.Description,
            variant.Code,
            specificationText,
            attributeText));

        return Regex.Matches(text, @"\d+(?:[\.,]\d+)?")
            .Select(match => decimal.TryParse(
                match.Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value)
                    ? value
                    : (decimal?)null)
            .OfType<decimal>()
            .Any(value => Math.Abs(value - targetSize) <= 0.5m);
    }

    private static bool ContainsSpecificationText(
        ProductVariant variant,
        IReadOnlyCollection<string> specificationKeys,
        IReadOnlyCollection<string> terms)
    {
        if (terms.Count == 0)
        {
            return false;
        }

        var text = string.Join(
            ' ',
            specificationKeys.Select(key => GetSpecificationText(variant, key)));

        return ContainsAny(text, terms);
    }

    private static bool ContainsProductText(
        ProductVariant variant,
        IReadOnlyCollection<string> terms)
    {
        var product = variant.Product;
        if (product is null)
        {
            return false;
        }

        var specificationText = string.Join(
            ' ',
            product.ProductSpecifications.Select(item => item.Value));
        var text = SearchTextNormalizer.Normalize(string.Join(
            ' ',
            product.Name,
            product.Description,
            product.Brand?.Name,
            variant.Code,
            specificationText));

        return ContainsAny(text, terms);
    }

    private static bool ContainsAny(
        string normalizedText,
        IReadOnlyCollection<string> terms)
    {
        return terms.Any(term =>
            normalizedText.Contains(
                SearchTextNormalizer.Normalize(term),
                StringComparison.Ordinal));
    }

    private static decimal? ParseFirstNumber(string value)
    {
        var match = Regex.Match(value, @"\d+(?:[\.,]\d+)?");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(
            match.Value.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var number)
            ? number
            : null;
    }

    private static IReadOnlyList<ProductCardViewModel> BuildProductCards(
        IReadOnlyList<ProductVariant> variants,
        string? sort)
    {
        var representatives = variants
            .GroupBy(BuildVariantGroupKey)
            .Select(group => group
                .OrderByDescending(variant => variant.IsDefault)
                .ThenByDescending(variant => variant.Quantity > 0)
                .ThenBy(variant => variant.Id)
                .First());

        representatives = NormalizeSort(sort) switch
        {
            "price-asc" => representatives
                .OrderBy(variant => variant.Price)
                .ThenBy(variant => variant.Product!.Name),
            "price-desc" => representatives
                .OrderByDescending(variant => variant.Price)
                .ThenBy(variant => variant.Product!.Name),
            "promotion" => representatives
                .OrderByDescending(variant => variant.Product!.IsFeatured)
                .ThenByDescending(variant => variant.SoldCount)
                .ThenByDescending(variant => variant.Product!.TotalSoldCount),
            _ => representatives
                .OrderByDescending(variant => variant.Product!.IsFeatured)
                .ThenByDescending(variant => variant.SoldCount)
                .ThenByDescending(variant => variant.Product!.TotalSoldCount)
                .ThenByDescending(variant => variant.Product!.ViewsCount)
                .ThenByDescending(variant => variant.IsDefault)
        };

        return representatives
            .Select(DbHomeProductCardMapper.ToProductCard)
            .OfType<ProductCardViewModel>()
            .ToList();
    }

    private static IReadOnlyList<ProductCardViewModel> GetProductPage(
        IReadOnlyList<ProductCardViewModel> products,
        int requestedPage)
    {
        var page = NormalizePage(requestedPage);

        return products
            .Skip((page - 1) * InitialProductCount)
            .Take(InitialProductCount)
            .ToList();
    }

    private static string BuildVariantGroupKey(ProductVariant variant)
    {
        var optionIds = variant.VariantAttributes
            .Where(attribute => !string.Equals(
                attribute.AttributeOption?.Attribute?.Code,
                CatalogAttributeCodes.Color,
                StringComparison.OrdinalIgnoreCase))
            .Select(attribute => attribute.AttributeOptionId)
            .OrderBy(id => id);

        return $"{variant.ProductId}:{string.Join('-', optionIds)}";
    }

    private static CategoryHotSaleViewModel BuildHotSale(
        IReadOnlyList<ProductVariant> variants,
        string? sort)
    {
        return new CategoryHotSaleViewModel
        {
            Title = "Sản phẩm nổi bật",
            Products = BuildProductCards(variants, sort).Take(8).ToList()
        };
    }

    private static CategoryFilterViewModel BuildEmptyFilter(
        Category category,
        int resultCount,
        CategoryPageRequest request)
    {
        return new CategoryFilterViewModel
        {
            Title = "Chọn theo tiêu chí",
            PrimaryItems = [],
            SecondaryItems = [],
            Groups = [],
            SortOptions = BuildSortOptions(category.Slug, request.Brand, request.Sort, request),
            CategorySlug = category.Slug,
            Brand = request.Brand,
            Sort = request.Sort,
            ActiveSelectionCount = 0,
            ResultCount = resultCount
        };
    }

    private static CategoryFilterViewModel BuildFilter(
        Category category,
        IReadOnlyList<Category> categories,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        int resultCount,
        CategoryPageRequest request)
    {
        if (string.Equals(category.Slug, "dien-thoai", StringComparison.OrdinalIgnoreCase))
        {
            return BuildPhoneFilter(category, filterSourceVariants, resultCount, request);
        }

        var categoryIds = GetSubtreeCategoryIds(category.Id, categories);
        var groups = BuildDynamicFilterGroups(
            categories,
            categoryIds,
            filterSourceVariants,
            request.Filters);
        if (IsTvCategory(category))
        {
            groups = [BuildTvScreenSizeFilterGroup(filterSourceVariants, request.Filters), ..groups];
        }

        var activeSelectionCount = (request.Filters?.Sum(item => item.Value.Count) ?? 0)
            + (request.InStockOnly ? 1 : 0)
            + (request.NewArrivalsOnly ? 1 : 0);

        return new CategoryFilterViewModel
        {
            Title = "Chọn theo tiêu chí",
            PrimaryItems = BuildFilterStateItems(category.Slug, request, activeSelectionCount),
            SecondaryItems = [],
            Groups = groups,
            SortOptions = BuildSortOptions(category.Slug, request.Brand, request.Sort, request),
            CategorySlug = category.Slug,
            Brand = request.Brand,
            Sort = request.Sort,
            ActiveSelectionCount = activeSelectionCount,
            ResultCount = resultCount
        };
    }

    private static IReadOnlyList<CategoryFilterGroupViewModel> BuildDynamicFilterGroups(
        IReadOnlyList<Category> categories,
        IReadOnlySet<long> categoryIds,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filters)
    {
        var selectedFilters = filters
            ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<CategoryFilterGroupViewModel>
        {
            BuildDynamicPriceFilterGroup(filterSourceVariants, selectedFilters)
        };

        var attributeDefinitions = categories
            .Where(item => categoryIds.Contains(item.Id))
            .SelectMany(item => item.CategoryVariantAttributes)
            .Where(item => item.Attribute is not null)
            .GroupBy(item => item.AttributeId)
            .Select(group => group.First().Attribute!)
            .OrderBy(attribute => attribute.Name)
            .ToList();

        foreach (var attribute in attributeDefinitions)
        {
            if (groups.Count >= MaxDynamicFilterGroups)
            {
                break;
            }

            var group = BuildAttributeFilterGroup(
                attribute,
                filterSourceVariants,
                selectedFilters);
            if (ShouldIncludeDynamicGroup(group))
            {
                groups.Add(group);
            }
        }

        var specificationDefinitions = categories
            .Where(item => categoryIds.Contains(item.Id))
            .SelectMany(item => item.CategorySpecifications)
            .Where(item => item.Specification is not null)
            .GroupBy(item => item.SpecificationId)
            .Select(group => group
                .OrderBy(item => item.SortOrder)
                .First())
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Specification!.Name)
            .ToList();

        foreach (var definition in specificationDefinitions)
        {
            if (groups.Count >= MaxDynamicFilterGroups)
            {
                break;
            }

            var group = BuildSpecificationFilterGroup(
                definition.Specification!,
                filterSourceVariants,
                selectedFilters);
            if (ShouldIncludeDynamicGroup(group))
            {
                groups.Add(group);
            }
        }

        return groups;
    }

    private static CategoryFilterGroupViewModel BuildDynamicPriceFilterGroup(
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyDictionary<string, IReadOnlyList<string>> selectedFilters)
    {
        var definition = PhoneFilterGroups.First(group => group.Key == "price");
        var selectedValues = GetSelectedFilterValues(selectedFilters, definition.Key);

        return new CategoryFilterGroupViewModel
        {
            Key = definition.Key,
            Label = definition.Label,
            Icon = definition.Icon,
            SelectedCount = selectedValues.Count,
            Options = definition.Options.Select(option => new CategoryFilterOptionViewModel
            {
                Value = option.Value,
                Label = option.Label,
                IsSelected = selectedValues.Contains(option.Value),
                IsAvailable = selectedValues.Contains(option.Value)
                    || filterSourceVariants.Any(variant =>
                        MatchesPrice(variant.Price, option.Value))
            }).ToList()
        };
    }

    private static CategoryFilterGroupViewModel BuildAttributeFilterGroup(
        Models.Entities.Attribute attribute,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyDictionary<string, IReadOnlyList<string>> selectedFilters)
    {
        var key = $"{AttributeFilterPrefix}{attribute.Id}";
        var selectedValues = GetSelectedFilterValues(selectedFilters, key);
        var options = filterSourceVariants
            .SelectMany(variant => variant.VariantAttributes)
            .Select(item => item.AttributeOption)
            .OfType<AttributeOption>()
            .Where(option => option.AttributeId == attribute.Id)
            .GroupBy(option => option.Id)
            .Select(group => group.First())
            .OrderBy(option => option.Label)
            .ThenBy(option => option.Value)
            .Select(option =>
            {
                var value = option.Id.ToString(CultureInfo.InvariantCulture);
                return new CategoryFilterOptionViewModel
                {
                    Value = value,
                    Label = string.IsNullOrWhiteSpace(option.Label) ? option.Value : option.Label,
                    IsSelected = selectedValues.Contains(value),
                    IsAvailable = true
                };
            })
            .ToList();

        return new CategoryFilterGroupViewModel
        {
            Key = key,
            Label = attribute.Name,
            SelectedCount = selectedValues.Count,
            Options = options
        };
    }

    private static CategoryFilterGroupViewModel BuildSpecificationFilterGroup(
        Specification specification,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyDictionary<string, IReadOnlyList<string>> selectedFilters)
    {
        var key = $"{SpecificationFilterPrefix}{specification.Id}";
        var selectedValues = GetSelectedFilterValues(selectedFilters, key);
        var options = filterSourceVariants
            .SelectMany(variant => variant.Product?.ProductSpecifications
                ?? Enumerable.Empty<ProductSpecification>())
            .Where(item => item.SpecificationId == specification.Id)
            .Select(item => item.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .Select(value => new CategoryFilterOptionViewModel
            {
                Value = value,
                Label = value,
                IsSelected = selectedValues.Contains(value),
                IsAvailable = true
            })
            .ToList();

        return new CategoryFilterGroupViewModel
        {
            Key = key,
            Label = specification.Name,
            SelectedCount = selectedValues.Count,
            Options = options
        };
    }

    private static HashSet<string> GetSelectedFilterValues(
        IReadOnlyDictionary<string, IReadOnlyList<string>> selectedFilters,
        string key)
    {
        return selectedFilters.TryGetValue(key, out var values)
            ? values.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ShouldIncludeDynamicGroup(CategoryFilterGroupViewModel group)
    {
        return group.Options.Count > 1
            && group.Options.Count <= MaxDynamicFilterOptions;
    }

    private static CategoryFilterGroupViewModel BuildTvScreenSizeFilterGroup(
        IReadOnlyList<ProductVariant> filterSourceVariants,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filters)
    {
        var selectedValues = filters is not null
            ? GetSelectedFilterValues(filters, ScreenSizeFilterKey)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new CategoryFilterGroupViewModel
        {
            Key = ScreenSizeFilterKey,
            Label = "Kích thước màn hình",
            SelectedCount = selectedValues.Count,
            Options = TvScreenSizeFilterOptions.Select(option =>
            {
                var isSelected = selectedValues.Contains(option.Value);
                return new CategoryFilterOptionViewModel
                {
                    Value = option.Value,
                    Label = option.Label,
                    IsSelected = isSelected,
                    IsAvailable = isSelected || filterSourceVariants.Any(variant =>
                        MatchesTvScreenSize(variant, option.Value))
                };
            }).ToList()
        };
    }

    private static IReadOnlyList<CategoryFilterItemViewModel> BuildFilterStateItems(
        string categorySlug,
        CategoryPageRequest request,
        int activeSelectionCount)
    {
        return
        [
            new()
            {
                Label = activeSelectionCount > 0
                    ? $"Bộ lọc ({activeSelectionCount})"
                    : "Bộ lọc",
                Url = BuildCatalogUrl(categorySlug, request.Brand, request.Sort),
                Icon = "filter",
                IsEmphasized = true,
                IsActive = activeSelectionCount > 0
            },
            new()
            {
                Label = "Sẵn hàng",
                Url = BuildCatalogStateUrl(
                    categorySlug,
                    request.Brand,
                    request.Sort,
                    request.Filters,
                    !request.InStockOnly,
                    request.NewArrivalsOnly),
                Icon = "truck",
                IsActive = request.InStockOnly
            },
            new()
            {
                Label = "Hàng mới về",
                Url = BuildCatalogStateUrl(
                    categorySlug,
                    request.Brand,
                    request.Sort,
                    request.Filters,
                    request.InStockOnly,
                    !request.NewArrivalsOnly),
                Icon = "box",
                IsActive = request.NewArrivalsOnly
            }
        ];
    }

    private static CategoryFilterViewModel BuildPhoneFilter(
        Category category,
        IReadOnlyList<ProductVariant> filterSourceVariants,
        int resultCount,
        CategoryPageRequest request)
    {
        var selectedFilters = request.Filters
            ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var groups = PhoneFilterGroups.Select(group =>
        {
            var selectedValues = selectedFilters.TryGetValue(group.Key, out var values)
                ? values.ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return new CategoryFilterGroupViewModel
            {
                Key = group.Key,
                Label = group.Label,
                Icon = group.Icon,
                SelectedCount = selectedValues.Count,
                Options = group.Options.Select(option =>
                {
                    var isSelected = selectedValues.Contains(option.Value);
                    return new CategoryFilterOptionViewModel
                    {
                        Value = option.Value,
                        Label = option.Label,
                        IsSelected = isSelected,
                        IsAvailable = isSelected || filterSourceVariants.Any(variant =>
                            MatchesPhoneFilter(variant, group.Key, option.Value))
                    };
                }).ToList()
            };
        }).ToList();

        var activeSelectionCount = groups.Sum(group => group.SelectedCount)
            + (request.InStockOnly ? 1 : 0)
            + (request.NewArrivalsOnly ? 1 : 0);

        return new CategoryFilterViewModel
        {
            Title = "Chọn theo tiêu chí",
            CategorySlug = category.Slug,
            Brand = request.Brand,
            Sort = request.Sort,
            ActiveSelectionCount = activeSelectionCount,
            ResultCount = resultCount,
            PrimaryItems = BuildFilterStateItems(category.Slug, request, activeSelectionCount),
            SecondaryItems = [],
            Groups = groups,
            SortOptions = BuildSortOptions(category.Slug, request.Brand, request.Sort, request)
        };
    }

    private static IReadOnlyList<CategorySortOptionViewModel> BuildSortOptions(
        string categorySlug,
        string? brand,
        string? sort,
        CategoryPageRequest? request = null)
    {
        var activeSort = NormalizeSort(sort);

        return
        [
            SortOption("Phổ biến", "popular", "star"),
            SortOption("Khuyến mãi HOT", "promotion", "discount"),
            SortOption("Giá Thấp - Cao", "price-asc", "sort-up"),
            SortOption("Giá Cao - Thấp", "price-desc", "sort-down")
        ];

        CategorySortOptionViewModel SortOption(string label, string value, string icon)
        {
            return new CategorySortOptionViewModel
            {
                Label = label,
                Url = request is null
                    ? BuildCatalogUrl(categorySlug, brand, value)
                    : BuildCatalogStateUrl(
                        categorySlug,
                        brand,
                        value,
                        request.Filters,
                        request.InStockOnly,
                        request.NewArrivalsOnly),
                Icon = icon,
                IsActive = string.Equals(value, activeSort, StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    private static IReadOnlyList<CategoryQuickLinkViewModel> BuildCategoryQuickLinks(
        IReadOnlyList<Category> categories,
        IReadOnlyList<ProductVariant> variants)
    {
        return categories
            .Select(category => BuildCategoryQuickLink(category, variants))
            .ToList();
    }

    private static CategoryQuickLinkViewModel BuildCategoryQuickLink(
        Category category,
        IReadOnlyList<ProductVariant> variants)
    {
        var imageUrl = NormalizeImageUrl(category.ImagePath);
        if (imageUrl == FallbackImageUrl)
        {
            imageUrl = variants
                .Where(variant => variant.Product?.CategoryId == category.Id)
                .Select(GetVariantImageUrl)
                .FirstOrDefault(url => url != FallbackImageUrl)
                ?? FallbackImageUrl;
        }

        return new CategoryQuickLinkViewModel
        {
            Label = category.Name,
            Url = BuildCatalogUrl(category.Slug),
            ImageUrl = imageUrl,
            ImageAlt = category.Name
        };
    }

    private static IReadOnlyList<CategoryBreadcrumbViewModel> BuildBreadcrumbs(
        Category category,
        IReadOnlyList<Category> categories)
    {
        var categoryById = categories.ToDictionary(item => item.Id);
        var ancestors = new Stack<Category>();
        var current = category;

        while (current.ParentId.HasValue
            && categoryById.TryGetValue(current.ParentId.Value, out var parent))
        {
            ancestors.Push(parent);
            current = parent;
        }

        List<CategoryBreadcrumbViewModel> breadcrumbs =
        [
            new() { Label = "Trang chủ", Url = "/" }
        ];

        while (ancestors.Count > 0)
        {
            var ancestor = ancestors.Pop();
            breadcrumbs.Add(new()
            {
                Label = ancestor.Name,
                Url = BuildCatalogUrl(ancestor.Slug)
            });
        }

        breadcrumbs.Add(new()
        {
            Label = category.Name,
            Url = BuildCatalogUrl(category.Slug),
            IsCurrent = true
        });

        return breadcrumbs;
    }

    private static CategorySeoContentViewModel BuildSeoContent(Category category)
    {
        var paragraphs = string.IsNullOrWhiteSpace(category.Description)
            ? new[]
            {
                $"Khám phá các sản phẩm {category.Name.ToLowerInvariant()} chính hãng tại TechStore.",
                "Danh sách sản phẩm, thương hiệu và tiêu chí lọc được cập nhật trực tiếp từ cơ sở dữ liệu."
            }
            : new[] { category.Description };

        return new CategorySeoContentViewModel
        {
            Title = $"{category.Name} chính hãng tại TechStore",
            Paragraphs = paragraphs
        };
    }

    private static QuestionAnswerSectionViewModel BuildQuestionAnswer(Category category)
    {
        return new QuestionAnswerSectionViewModel
        {
            Title = "Hỏi và đáp",
            FormTitle = $"Bạn cần tư vấn về {category.Name.ToLowerInvariant()}?",
            Description = "Hãy gửi nhu cầu và ngân sách dự kiến để TechStore hỗ trợ lựa chọn sản phẩm phù hợp.",
            Placeholder = "Viết câu hỏi của bạn tại đây",
            SubmitLabel = "Gửi câu hỏi",
            Threads = []
        };
    }

    private static IReadOnlyList<Category> GetDirectChildren(
        long categoryId,
        IReadOnlyList<Category> categories)
    {
        return categories
            .Where(category => category.ParentId == categoryId)
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Id)
            .ToList();
    }

    private static IReadOnlySet<long> GetSubtreeCategoryIds(
        long categoryId,
        IReadOnlyList<Category> categories)
    {
        var result = new HashSet<long> { categoryId };
        var pending = new Queue<long>();
        pending.Enqueue(categoryId);

        while (pending.Count > 0)
        {
            var parentId = pending.Dequeue();
            foreach (var child in categories.Where(category => category.ParentId == parentId))
            {
                if (result.Add(child.Id))
                {
                    pending.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    private static Category? ResolveRequestedCategory(
        IReadOnlyList<Category> categories,
        string requestedSlug)
    {
        var category = categories.FirstOrDefault(item =>
            string.Equals(item.Slug, requestedSlug, StringComparison.OrdinalIgnoreCase));

        if (category is not null || !IsTvCategorySlug(requestedSlug))
        {
            return category;
        }

        return categories.FirstOrDefault(item => IsTvCategorySlug(item.Slug));
    }

    private static string GetVariantImageUrl(ProductVariant variant)
    {
        var imagePath = variant.ProductVariantImages
            .OrderBy(image => image.Position)
            .ThenBy(image => image.Id)
            .Select(image => image.ImagePath)
            .FirstOrDefault();

        return NormalizeImageUrl(imagePath);
    }

    private static string BuildMetaDescription(Category category)
    {
        return string.IsNullOrWhiteSpace(category.Description)
            ? $"Mua {category.Name.ToLowerInvariant()} chính hãng, giá tốt tại TechStore."
            : category.Description;
    }

    private static string NormalizeCategorySlug(string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return CategoryAliases.TryGetValue(normalizedSlug, out var mappedSlug)
            ? mappedSlug
            : normalizedSlug;
    }

    private static string BuildCacheKey(CategoryPageRequest request)
    {
        var filters = request.Filters is null
            ? string.Empty
            : string.Join(
                ";",
                request.Filters
                    .OrderBy(filter => filter.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(filter => $"{filter.Key}={string.Join(",", filter.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}"));

        return string.Join(
            "|",
            "catalog-page-v2",
            NormalizeCategorySlug(request.Slug),
            request.Brand?.Trim().ToLowerInvariant() ?? string.Empty,
            NormalizeSort(request.Sort),
            request.InStockOnly,
            request.NewArrivalsOnly,
            NormalizePage(request.Page),
            filters);
    }

    private static bool IsTvCategory(Category category)
    {
        return IsTvCategorySlug(category.Slug);
    }

    private static bool IsTvCategorySlug(string slug)
    {
        return string.Equals(slug, "tv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slug, "tivi", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSort(string? sort)
    {
        return string.IsNullOrWhiteSpace(sort)
            ? "popular"
            : sort.Trim().ToLowerInvariant();
    }

    private static int NormalizePage(int page)
    {
        return Math.Max(1, page);
    }

    private static string BuildCatalogUrl(
        string categorySlug,
        string? brand = null,
        string? sort = null,
        string? extraQuery = null)
    {
        var query = new List<string>
        {
            $"cat={Uri.EscapeDataString(categorySlug)}"
        };

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query.Add($"brand={Uri.EscapeDataString(brand)}");
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            query.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        if (!string.IsNullOrWhiteSpace(extraQuery))
        {
            query.Add(extraQuery);
        }

        return $"/catalog?{string.Join('&', query)}";
    }

    private static string BuildCatalogStateUrl(
        string categorySlug,
        string? brand,
        string? sort,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? filters,
        bool inStock,
        bool isNew)
    {
        var query = new List<string>
        {
            $"cat={Uri.EscapeDataString(categorySlug)}"
        };

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query.Add($"brand={Uri.EscapeDataString(brand)}");
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            query.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        if (filters is not null)
        {
            foreach (var filter in filters.OrderBy(item => item.Key))
            {
                foreach (var value in filter.Value
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    query.Add(
                        $"f_{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(value)}");
                }
            }
        }

        if (inStock)
        {
            query.Add("inStock=true");
        }

        if (isNew)
        {
            query.Add("isNew=true");
        }

        return $"/catalog?{string.Join('&', query)}";
    }

    private static string? NormalizeOptionalImageUrl(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath)
            ? null
            : NormalizeImageUrl(imagePath);
    }

    private static string NormalizeImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return FallbackImageUrl;
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

    private sealed record PhoneFilterGroupDefinition(
        string Key,
        string Label,
        string? Icon,
        IReadOnlyList<PhoneFilterOptionDefinition> Options);

    private sealed record PhoneFilterOptionDefinition(string Value, string Label);
}
