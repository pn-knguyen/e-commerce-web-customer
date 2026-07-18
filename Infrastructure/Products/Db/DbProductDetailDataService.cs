using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Caching;
using e_commerce_web_customer.Models.Constants;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.ViewModels.Product;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Infrastructure.Products.Db;

public sealed class DbProductDetailDataService(
    EcommerceDbContext dbContext,
    IProductRecommendationService productRecommendationService,
    IMemoryCache cache,
    StorefrontDbQueryGate dbQueryGate) : IProductDetailDataService
{
    private const string FallbackImageUrl = "/images/logo-techstore-icon.svg";
    private const string DefaultSpecGroupName = "Thông tin sản phẩm";
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public Task<ProductDetailViewModel?> CreateProductDetailAsync(
        string slug,
        string? variantKey = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Task.FromResult<ProductDetailViewModel?>(null);
        }

        var normalizedVariantKey = variantKey?.Trim().ToLowerInvariant() ?? "default";
        var cacheKey = $"product-detail-v2:{normalizedSlug}:{normalizedVariantKey}";

        return cache.GetOrCreateExclusiveAsync(
            cacheKey,
            () => dbQueryGate.RunAsync(
                () => CreateProductDetailUncachedAsync(
                    normalizedSlug,
                    variantKey,
                    CancellationToken.None)),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            });
    }

    private async Task<ProductDetailViewModel?> CreateProductDetailUncachedAsync(
        string normalizedSlug,
        string? variantKey,
        CancellationToken cancellationToken)
    {

        var selectedProduct = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.Slug == normalizedSlug)
            .Include(product => product.Brand)
            .Include(product => product.Category)
                .ThenInclude(category => category!.CategorySpecifications)
                    .ThenInclude(categorySpecification => categorySpecification.Specification)
            .Include(product => product.ProductSpecifications)
                .ThenInclude(productSpecification => productSpecification.Specification)
            .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages)
            .Include(product => product.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.VariantAttributes)
                    .ThenInclude(variantAttribute => variantAttribute.AttributeOption)
                        .ThenInclude(attributeOption => attributeOption!.Attribute)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (selectedProduct is null)
        {
            return null;
        }

        IReadOnlyList<Product> familyProducts = [selectedProduct];

        var activeVariants = familyProducts
            .SelectMany(product => product.ProductVariants.Where(variant => variant.IsActive))
            .ToList();

        if (activeVariants.Count == 0)
        {
            return null;
        }

        var selectedVariant = ResolveSelectedVariant(
            activeVariants,
            selectedProduct.Id,
            variantKey);

        if (selectedVariant is null)
        {
            return null;
        }

        selectedProduct = familyProducts.FirstOrDefault(product => product.Id == selectedVariant.ProductId)
            ?? selectedProduct;

        var versionGroups = BuildVersionGroups(familyProducts, activeVariants);
        var selectedVersionKey = BuildVersionKey(selectedVariant);
        var selectedVersionGroup = versionGroups.FirstOrDefault(group => group.Key.Equals(selectedVersionKey));

        var colorVariants = selectedVersionGroup?.Variants
            ?? [selectedVariant];
        var detailName = BuildDetailName(selectedProduct, selectedVariant);
        var imageVariant = ResolveImageVariant(selectedVariant, colorVariants);
        var selectedImage = GetPrimaryImage(imageVariant);
        var mainImageUrl = NormalizeImageUrl(selectedImage?.ImagePath);
        var reviewSummary = BuildReviewSummary(
            detailName,
            await GetApprovedReviewsAsync(selectedProduct.Id, cancellationToken));
        var accessoryUpsells = await productRecommendationService.GetAccessoryUpsellsAsync(
            new ProductRecommendationRequest
            {
                ProductId = selectedProduct.Id,
                ProductName = selectedProduct.Name,
                ProductSlug = selectedProduct.Slug,
                BrandSlug = selectedProduct.Brand?.Slug,
                CategorySlug = selectedProduct.Category?.Slug,
                Surface = RecommendationSurface.ProductDetailAccessoryUpsell,
                Limit = 6
            },
            cancellationToken);

        return new ProductDetailViewModel
        {
            Slug = selectedProduct.Slug,
            CartItemId = GetVariantKey(selectedVariant),
            SelectedVariantLabel = BuildCartVariantLabel(selectedVariant),
            Name = detailName,
            Brand = selectedProduct.Brand?.Name ?? string.Empty,
            MainImageUrl = mainImageUrl,
            MainImageAlt = BuildImageAlt(selectedImage, detailName, imageVariant),
            CurrentPrice = selectedVariant.Price,
            OldPrice = null,
            IsAvailable = selectedVariant.Quantity > 0,
            StockStatusText = BuildStockStatusText(selectedVariant),
            Rating = reviewSummary.Score,
            ReviewCount = reviewSummary.TotalReviews,
            Breadcrumbs = BuildBreadcrumbs(selectedProduct, detailName),
            QuickLinks = BuildQuickLinks(),
            GalleryItems = BuildGalleryItems(selectedVariant, colorVariants, detailName),
            StorageOptions = BuildStorageOptions(versionGroups, selectedVersionKey, selectedVariant),
            ColorOptions = BuildColorOptions(selectedProduct, colorVariants, selectedVariant, detailName),
            VariantSpecRows = BuildVariantSpecRows(selectedVariant),
            TechnicalSpecSections = BuildTechnicalSpecSections(selectedProduct, detailName),
            AccessoryUpsells = accessoryUpsells
                .Select(accessory => new ProductAccessoryUpsellViewModel
                {
                    ProductVariantKey = accessory.ProductVariantKey,
                    Url = accessory.Url,
                    Name = accessory.Name,
                    ImageUrl = accessory.ImageUrl,
                    ImageAlt = accessory.ImageAlt,
                    MemberOffer = accessory.MemberOffer,
                    CurrentPrice = accessory.CurrentPrice,
                    OldPrice = accessory.OldPrice,
                    Variants = accessory.Variants
                        .Select(variant => new ProductAccessoryUpsellVariantViewModel
                        {
                            ProductVariantKey = variant.ProductVariantKey,
                            Url = variant.Url,
                            Label = variant.Label,
                            ImageUrl = variant.ImageUrl,
                            ImageAlt = variant.ImageAlt,
                            CurrentPrice = variant.CurrentPrice,
                            Quantity = variant.Quantity,
                            IsDefault = variant.IsDefault
                        })
                        .ToList()
                })
                .ToList(),
            RelatedProductGroups = BuildRelatedProductGroups(activeVariants, selectedProduct, selectedVariant),
            ReviewSummary = reviewSummary,
            QuestionAnswerSection = BuildQuestionAnswerSection(detailName)
        };
    }

    private async Task<IReadOnlyList<Rating>> GetApprovedReviewsAsync(
        long productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Ratings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(rating => rating.User)
            .Include(rating => rating.OrderItem)
                .ThenInclude(item => item!.ProductVariant)
                    .ThenInclude(variant => variant!.VariantAttributes)
                        .ThenInclude(attribute => attribute.AttributeOption)
                            .ThenInclude(option => option!.Attribute)
            .Where(rating => rating.IsApproved)
            .Where(rating => rating.OrderItem != null
                && rating.OrderItem.ProductVariant != null
                && rating.OrderItem.ProductVariant.ProductId == productId)
            .OrderByDescending(rating => rating.UpdatedAt ?? rating.CreatedAt)
            .ThenByDescending(rating => rating.Id)
            .ToListAsync(cancellationToken);
    }

    private static ProductVariant? ResolveSelectedVariant(
        IReadOnlyList<ProductVariant> activeVariants,
        long selectedProductId,
        string? variantKey)
    {
        if (!string.IsNullOrWhiteSpace(variantKey))
        {
            var normalizedVariantKey = variantKey.Trim();
            var variantByKey = activeVariants.FirstOrDefault(variant =>
                string.Equals(variant.Code, normalizedVariantKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(variant.Id.ToString(CultureInfo.InvariantCulture), normalizedVariantKey, StringComparison.OrdinalIgnoreCase));

            if (variantByKey is not null)
            {
                return variantByKey;
            }
        }

        return activeVariants
                .Where(variant => variant.ProductId == selectedProductId)
                .OrderByDescending(variant => variant.IsDefault)
                .ThenBy(variant => variant.Id)
                .FirstOrDefault()
            ?? activeVariants
                .OrderByDescending(variant => variant.IsDefault)
                .ThenBy(variant => variant.Id)
                .FirstOrDefault();
    }

    private static IReadOnlyList<VersionGroup> BuildVersionGroups(
        IReadOnlyList<Product> familyProducts,
        IReadOnlyList<ProductVariant> activeVariants)
    {
        var productById = familyProducts.ToDictionary(product => product.Id);
        var productOrderById = familyProducts
            .Select((product, index) => new { product.Id, Index = index })
            .ToDictionary(product => product.Id, product => product.Index);

        return activeVariants
            .GroupBy(BuildVersionKey)
            .Select(group => new VersionGroup(
                group.Key,
                productById[group.Key.ProductId],
                group.OrderBy(GetColorOrder).ThenBy(variant => variant.Id).ToList()))
            .OrderBy(group => productOrderById[group.Key.ProductId])
            .ThenBy(group => group.Key.StorageSize)
            .ThenBy(group => group.Key.RamSize)
            .ThenBy(group => group.Key.Label)
            .ToList();
    }

    private static ProductVariant ResolveImageVariant(
        ProductVariant selectedVariant,
        IReadOnlyList<ProductVariant> colorVariants)
    {
        if (GetPrimaryImage(selectedVariant) is not null)
        {
            return selectedVariant;
        }

        return colorVariants.FirstOrDefault(variant => GetPrimaryImage(variant) is not null)
            ?? selectedVariant;
    }

    private static VersionKey BuildVersionKey(ProductVariant variant)
    {
        var attributes = GetVersionAttributeValues(variant);
        var ram = attributes.FirstOrDefault(attribute => IsAttributeCode(
            attribute,
            CatalogAttributeCodes.Ram,
            CatalogAttributeCodes.RamCapacity));
        var storage = attributes.FirstOrDefault(attribute => IsAttributeCode(
            attribute,
            CatalogAttributeCodes.Rom,
            CatalogAttributeCodes.Storage,
            CatalogAttributeCodes.InternalStorage));
        var signature = attributes.Count == 0
            ? "default"
            : string.Join(
                '|',
                attributes.Select(attribute => attribute.OptionId.ToString(CultureInfo.InvariantCulture)));
        var label = BuildVersionAttributeLabel(attributes);

        return new VersionKey(
            variant.ProductId,
            signature,
            label,
            ParseCapacityToMb(ram?.Label),
            ParseCapacityToMb(storage?.Label));
    }

    private static IReadOnlyList<VariantAttributeValue> GetVersionAttributeValues(ProductVariant variant)
    {
        return variant.VariantAttributes
            .Select(variantAttribute =>
            {
                var option = variantAttribute.AttributeOption;
                var attribute = option?.Attribute;
                if (option is null || attribute is null)
                {
                    return null;
                }

                var label = string.IsNullOrWhiteSpace(option.Label)
                    ? option.Value
                    : option.Label;

                return new VariantAttributeValue(
                    variantAttribute.AttributeOptionId,
                    attribute.Code,
                    attribute.Name,
                    option.Value,
                    label);
            })
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!)
            .Where(attribute => !IsColorAttribute(attribute.Code, attribute.Name))
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Label))
            .OrderBy(attribute => GetAttributeDisplayOrder(
                attribute.Code,
                attribute.Name,
                attribute.Value,
                attribute.Label))
            .ThenBy(attribute => attribute.OptionId)
            .DistinctBy(attribute => attribute.OptionId)
            .ToList();
    }

    private static string BuildVersionAttributeLabel(IReadOnlyList<VariantAttributeValue> attributes)
    {
        var parts = new List<string>();
        foreach (var attribute in attributes)
        {
            AddVariantLabel(parts, attribute.Label);
        }

        return string.Join(' ', parts);
    }

    private static IReadOnlyList<ProductDetailStorageOptionViewModel> BuildStorageOptions(
        IReadOnlyList<VersionGroup> versionGroups,
        VersionKey selectedVersionKey,
        ProductVariant selectedVariant)
    {
        return versionGroups
            .Select(group =>
            {
                var targetVariant = ResolveVersionLinkVariant(group, selectedVariant);

                return new ProductDetailStorageOptionViewModel
                {
                    Label = BuildVersionLabel(group.Key),
                    Url = BuildVariantDetailUrl(group.Product, targetVariant),
                    IsActive = group.Key.Equals(selectedVersionKey),
                    IsInitiallyHidden = false,
                    IsAvailable = targetVariant.Quantity > 0,
                    StockStatusText = BuildStockStatusText(targetVariant)
                };
            })
            .ToList();
    }

    private static ProductVariant ResolveVersionLinkVariant(
        VersionGroup group,
        ProductVariant selectedVariant)
    {
        var selectedColor = GetColorLabel(selectedVariant);
        if (!string.IsNullOrWhiteSpace(selectedColor))
        {
            var sameColorVariant = group.Variants.FirstOrDefault(variant =>
                string.Equals(GetColorLabel(variant), selectedColor, StringComparison.OrdinalIgnoreCase));

            if (sameColorVariant is not null)
            {
                return sameColorVariant;
            }
        }

        return group.Variants.FirstOrDefault(variant => variant.IsDefault)
            ?? group.Variants[0];
    }

    private static IReadOnlyList<ProductDetailColorOptionViewModel> BuildColorOptions(
        Product product,
        IReadOnlyList<ProductVariant> colorVariants,
        ProductVariant selectedVariant,
        string detailName)
    {
        return colorVariants
            .OrderBy(GetColorOrder)
            .ThenBy(variant => variant.Id)
            .Select(variant =>
            {
                var image = GetPrimaryImage(variant);

                return new ProductDetailColorOptionViewModel
                {
                    VariantKey = GetVariantKey(variant),
                    DetailUrl = BuildVariantDetailUrl(product, variant),
                    VariantLabel = BuildCartVariantLabel(variant),
                    Name = BuildColorName(variant),
                    ImageUrl = NormalizeImageUrl(image?.ImagePath),
                    ImageAlt = BuildImageAlt(image, detailName, variant),
                    Price = variant.Price,
                    IsActive = variant.Id == selectedVariant.Id,
                    IsAvailable = variant.Quantity > 0,
                    StockStatusText = BuildStockStatusText(variant)
                };
            })
            .ToList();
    }

    private static string BuildColorName(ProductVariant variant)
    {
        return GetColorLabel(variant) ?? "Mặc định";
    }

    private static string? GetColorLabel(ProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.ColorName))
        {
            return variant.ColorName.Trim();
        }

        return variant.VariantAttributes
            .Select(variantAttribute => variantAttribute.AttributeOption)
            .Where(option => option?.Attribute is not null)
            .FirstOrDefault(option => IsColorAttribute(
                option!.Attribute!.Code,
                option.Attribute.Name))
            is { } colorOption
                ? string.IsNullOrWhiteSpace(colorOption.Label)
                    ? colorOption.Value
                    : colorOption.Label
                : null;
    }

    private static bool IsAttributeCode(VariantAttributeValue attribute, params string[] attributeCodes)
    {
        return attributeCodes.Any(code => string.Equals(
            code,
            attribute.Code,
            StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsColorAttribute(string? code, string? name)
    {
        if (string.Equals(code, CatalogAttributeCodes.Color, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalizedName = RemoveDiacritics(name ?? string.Empty).ToLowerInvariant();
        return normalizedName.Contains("color", StringComparison.Ordinal)
            || normalizedName.Contains("mau", StringComparison.Ordinal);
    }

    private static int GetAttributeDisplayOrder(
        string? code,
        string? name,
        string? value,
        string? label)
    {
        var normalizedCode = code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedCode is CatalogAttributeCodes.Ram or CatalogAttributeCodes.RamCapacity)
        {
            return 0;
        }

        if (normalizedCode is CatalogAttributeCodes.Rom
            or CatalogAttributeCodes.Storage
            or CatalogAttributeCodes.InternalStorage)
        {
            return 1;
        }

        var searchableText = RemoveDiacritics(string.Join(' ', code, name, value, label)).ToLowerInvariant();
        if (ContainsAny(searchableText, CatalogAttributeCodes.Ram, "bo nho ram"))
        {
            return 0;
        }

        if (ContainsAny(
            searchableText,
            CatalogAttributeCodes.Rom,
            CatalogAttributeCodes.Storage,
            CatalogAttributeCodes.InternalStorage,
            "internal-storage",
            "capacity",
            "dung luong",
            "luu tru",
            "bo nho trong"))
        {
            return 1;
        }

        return 100;
    }

    private static IReadOnlyList<ProductTechnicalSpecRowViewModel> BuildVariantSpecRows(ProductVariant selectedVariant)
    {
        var rows = selectedVariant.VariantAttributes
            .Select(variantAttribute => variantAttribute.AttributeOption)
            .Where(option => option?.Attribute is not null)
            .OrderBy(option => option!.Attribute!.Name)
            .ThenBy(option => option!.Id)
            .Select(option => new ProductTechnicalSpecRowViewModel
            {
                Label = option!.Attribute!.Name,
                Value = string.IsNullOrWhiteSpace(option.Label) ? option.Value : option.Label,
                IsHighlighted = true
            })
            .Where(row => !string.IsNullOrWhiteSpace(row.Value) && !IsColorSpecLabel(row.Label))
            .DistinctBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    private static bool IsColorSpecLabel(string label)
    {
        return label.Contains("màu", StringComparison.OrdinalIgnoreCase)
            || label.Contains("color", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildStockStatusText(ProductVariant variant)
    {
        return variant.Quantity > 0
            ? "Còn hàng"
            : "Hết hàng";
    }

    private static IReadOnlyList<ProductDetailGalleryItemViewModel> BuildGalleryItems(
        ProductVariant selectedVariant,
        IReadOnlyList<ProductVariant> colorVariants,
        string detailName)
    {
        var defaultVariant = colorVariants.FirstOrDefault(variant => variant.IsDefault)
            ?? selectedVariant;
        var orderedVariants = new[] { selectedVariant, defaultVariant }
            .Concat(colorVariants.OrderBy(GetColorOrder).ThenBy(variant => variant.Id))
            .DistinctBy(variant => variant.Id);

        var galleryItems = orderedVariants
            .SelectMany(variant => variant.ProductVariantImages
                .OrderBy(image => image.Position)
                .ThenBy(image => image.Id)
                .Select((image, index) => new ProductDetailGalleryItemViewModel
                {
                    Label = BuildGalleryLabel(variant, index),
                    ImageUrl = NormalizeImageUrl(image.ImagePath),
                    ImageAlt = BuildImageAlt(image, detailName, variant)
                }))
            .DistinctBy(item => item.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (galleryItems.Count > 0)
        {
            return galleryItems;
        }

        return
        [
            new()
            {
                Label = "Sản phẩm",
                ImageUrl = FallbackImageUrl,
                ImageAlt = detailName
            }
        ];
    }

    private static string BuildGalleryLabel(ProductVariant variant, int imageIndex)
    {
        if (string.IsNullOrWhiteSpace(variant.ColorName))
        {
            return $"Ảnh {imageIndex + 1}";
        }

        return imageIndex == 0
            ? variant.ColorName
            : $"{variant.ColorName} {imageIndex + 1}";
    }

    private static IReadOnlyList<ProductTechnicalSpecSectionViewModel> BuildTechnicalSpecSections(
        Product product,
        string detailName)
    {
        var categorySpecs = product.Category?.CategorySpecifications
                .Where(categorySpecification => categorySpecification.Specification is not null)
                .ToDictionary(categorySpecification => categorySpecification.SpecificationId)
            ?? [];

        var rows = product.ProductSpecifications
            .Where(productSpecification => productSpecification.Specification is not null)
            .Select(productSpecification =>
            {
                categorySpecs.TryGetValue(productSpecification.SpecificationId, out var categorySpecification);
                var specification = productSpecification.Specification!;
                var groupName = string.IsNullOrWhiteSpace(categorySpecification?.GroupName)
                    ? DefaultSpecGroupName
                    : categorySpecification.GroupName;
                var rowSortOrder = categorySpecification?.SortOrder ?? productSpecification.SortOrder;

                return new SpecRowDraft(
                    groupName,
                    rowSortOrder,
                    new ProductTechnicalSpecRowViewModel
                    {
                        Label = specification.Name,
                        Value = FormatSpecificationValue(productSpecification.Value, specification.Unit),
                        IsHighlighted = productSpecification.IsHighlight
                    },
                    rowSortOrder);
            })
            .ToList();

        if (rows.Count == 0)
        {
            return
            [
                new()
                {
                    Id = "thong-tin-san-pham",
                    Title = DefaultSpecGroupName,
                    Rows =
                    [
                        new()
                        {
                            Label = "Tên sản phẩm",
                            Value = detailName,
                            IsHighlighted = true
                        }
                    ]
                }
            ];
        }

        return rows
            .GroupBy(row => row.GroupName)
            .OrderBy(group => group.Min(row => row.GroupSortOrder))
            .ThenBy(group => group.Key)
            .Select(group => new ProductTechnicalSpecSectionViewModel
            {
                Id = Slugify(group.Key),
                Title = group.Key,
                Rows = group
                    .OrderBy(row => row.RowSortOrder)
                    .ThenBy(row => row.Row.Label)
                    .Select(row => row.Row)
                    .ToList()
            })
            .ToList();
    }

    private static IReadOnlyList<ProductRelatedProductGroupViewModel> BuildRelatedProductGroups(
        IReadOnlyList<ProductVariant> activeVariants,
        Product product,
        ProductVariant selectedVariant)
    {
        var products = activeVariants
            .Where(variant => variant.ProductId == product.Id && variant.Id != selectedVariant.Id)
            .OrderBy(variant => BuildVersionKey(variant).StorageSize)
            .ThenBy(variant => BuildVersionKey(variant).RamSize)
            .ThenBy(variant => BuildVersionKey(variant).Label)
            .ThenBy(GetColorOrder)
            .ThenBy(variant => variant.Id)
            .Select(variant =>
            {
                var image = GetPrimaryImage(variant);
                var name = BuildRelatedVariantName(product, variant);

                return new ProductRelatedProductViewModel
                {
                    ProductVariantKey = GetVariantKey(variant),
                    Url = BuildVariantDetailUrl(product, variant),
                    Name = name,
                    ImageUrl = NormalizeImageUrl(image?.ImagePath),
                    ImageAlt = BuildImageAlt(image, name, variant),
                    CurrentPrice = variant.Price,
                    OldPrice = null,
                    DiscountLabel = null,
                    InstallmentLabel = "Trả góp 0%",
                    GiftNote = null,
                    DeliveryLabel = "Giao 2 giờ",
                    Location = "Hồ Chí Minh",
                    Rating = product.RatingAverage > 0 ? product.RatingAverage : null
                };
            })
            .Take(12)
            .ToList();

        if (products.Count == 0)
        {
            return [];
        }

        return
        [
            new()
            {
                Id = "same-product-variants",
                Label = "Phiên bản cùng sản phẩm",
                IsActive = true,
                Products = products
            }
        ];
    }

    private static IReadOnlyList<ProductDetailBreadcrumbViewModel> BuildBreadcrumbs(
        Product product,
        string detailName)
    {
        List<ProductDetailBreadcrumbViewModel> breadcrumbs =
        [
            new() { Label = "Trang chủ", Url = "/" }
        ];

        if (product.Category is not null)
        {
            breadcrumbs.Add(new()
            {
                Label = product.Category.Name,
                Url = $"/catalog?cat={Uri.EscapeDataString(product.Category.Slug)}"
            });
        }

        if (product.Brand is not null)
        {
            breadcrumbs.Add(new()
            {
                Label = product.Brand.Name,
                Url = product.Category is null
                    ? $"/catalog?brand={Uri.EscapeDataString(product.Brand.Slug)}"
                    : $"/catalog?cat={Uri.EscapeDataString(product.Category.Slug)}&brand={Uri.EscapeDataString(product.Brand.Slug)}"
            });
        }

        breadcrumbs.Add(new() { Label = detailName });

        return breadcrumbs;
    }

    private static IReadOnlyList<ProductDetailActionLinkViewModel> BuildQuickLinks()
    {
        return
        [
            new() { Label = "Yêu thích", IconId = "product-card-icon-heart" },
            new() { Label = "Hỏi đáp", IconId = "hero-icon-news", Url = "#block-comment-cps" },
            new() { Label = "Thông số", IconId = "hero-icon-phone" },
            new() { Label = "So sánh", IconId = "hero-icon-swap" }
        ];
    }

    private static ProductReviewSummaryViewModel BuildReviewSummary(
        string detailName,
        IReadOnlyList<Rating> reviews)
    {
        var totalReviews = reviews.Count;
        var score = totalReviews == 0
            ? 0m
            : decimal.Round(reviews.Average(review => (decimal)review.Stars), 1, MidpointRounding.AwayFromZero);

        return new ProductReviewSummaryViewModel
        {
            Title = $"Đánh giá {detailName}",
            Score = score,
            TotalReviews = totalReviews,
            RatingBreakdown = Enumerable.Range(1, 5)
                .Reverse()
                .Select(star =>
                {
                    var count = reviews.Count(review => review.Stars == star);
                    return new ProductRatingBreakdownViewModel
                    {
                        Stars = star,
                        Count = count,
                        Percent = totalReviews == 0
                            ? 0
                            : (int)Math.Round(count * 100m / totalReviews, MidpointRounding.AwayFromZero)
                    };
                })
                .ToList(),
            ExperienceRatings = [],
            Reviews = reviews
                .Take(20)
                .Select(ToReviewViewModel)
                .ToList()
        };
    }

    private static ProductReviewViewModel ToReviewViewModel(Rating rating)
    {
        var author = ResolveReviewAuthor(rating.User);
        var variantLabel = rating.OrderItem?.ProductVariant is null
            ? string.Empty
            : BuildCartVariantLabel(rating.OrderItem.ProductVariant);

        var tags = new List<string> { "Đã mua hàng" };
        if (!string.IsNullOrWhiteSpace(variantLabel))
        {
            tags.Add(variantLabel);
        }

        return new ProductReviewViewModel
        {
            Author = author,
            Initial = ResolveReviewInitial(author),
            Rating = rating.Stars,
            RatingText = ResolveRatingText(rating.Stars),
            Content = string.IsNullOrWhiteSpace(rating.Comment)
                ? $"Khách hàng đã chấm {rating.Stars}/5 sao cho sản phẩm này."
                : rating.Comment.Trim(),
            TimeAgo = FormatReviewTime(rating.UpdatedAt ?? rating.CreatedAt),
            Tags = tags
        };
    }

    private static string ResolveReviewAuthor(User? user)
    {
        var name = user?.FullName?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var username = user?.Username?.Trim();
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        var email = user?.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? email;
        }

        return "Khách hàng TechStore";
    }

    private static string ResolveReviewInitial(string author)
    {
        var first = author.Trim().FirstOrDefault(char.IsLetterOrDigit);
        return first == default
            ? "T"
            : first.ToString().ToUpper(ViCulture);
    }

    private static string ResolveRatingText(int stars) => stars switch
    {
        >= 5 => "Tuyệt vời",
        4 => "Hài lòng",
        3 => "Ổn",
        2 => "Chưa hài lòng",
        _ => "Không hài lòng"
    };

    private static string FormatReviewTime(DateTime value)
    {
        var localDate = value.ToLocalTime().Date;
        var days = (DateTime.Now.Date - localDate).Days;

        return days switch
        {
            <= 0 => "Hôm nay",
            1 => "Hôm qua",
            < 30 => $"{days} ngày trước",
            _ => localDate.ToString("dd/MM/yyyy", ViCulture)
        };
    }

    private static QuestionAnswerSectionViewModel BuildQuestionAnswerSection(string detailName)
    {
        return new QuestionAnswerSectionViewModel
        {
            Title = "Hỏi và đáp",
            FormTitle = "Hãy đặt câu hỏi cho chúng tôi",
            Description = $"Gửi câu hỏi về {detailName}, TechStore sẽ hỗ trợ bạn sớm nhất.",
            Placeholder = "Viết câu hỏi của bạn tại đây",
            SubmitLabel = "Gửi câu hỏi",
            AdditionalCommentCount = 0,
            Threads = []
        };
    }

    private static string BuildDetailName(Product product, ProductVariant variant)
    {
        var parts = new List<string> { product.Name };

        foreach (var attribute in GetVersionAttributeValues(variant))
        {
            AddVariantLabel(parts, attribute.Label);
        }

        return string.Join(' ', parts);
    }

    private static string BuildCartVariantLabel(ProductVariant variant)
    {
        var parts = new List<string>();

        foreach (var attribute in GetVersionAttributeValues(variant))
        {
            AddVariantLabel(parts, attribute.Label);
        }

        var colorLabel = GetColorLabel(variant);
        if (!string.IsNullOrWhiteSpace(colorLabel))
        {
            AddVariantLabel(parts, colorLabel);
        }

        return string.Join(" - ", parts);
    }

    private static string BuildVersionLabel(VersionKey versionKey)
    {
        return string.IsNullOrWhiteSpace(versionKey.Label)
            ? "Phiên bản tiêu chuẩn"
            : versionKey.Label;
    }

    private static string BuildRelatedVariantName(Product product, ProductVariant variant)
    {
        var parts = new List<string> { BuildDetailName(product, variant) };
        AddVariantLabel(parts, GetColorLabel(variant));

        return string.Join(' ', parts);
    }

    private static string BuildShortProductName(Product product)
    {
        var name = product.Name;

        if (!string.IsNullOrWhiteSpace(product.Brand?.Name)
            && name.StartsWith(product.Brand.Name, StringComparison.OrdinalIgnoreCase))
        {
            name = name[product.Brand.Name.Length..];
        }

        name = Regex.Replace(name, "\\bGalaxy\\b", string.Empty, RegexOptions.IgnoreCase);
        name = Regex.Replace(name, "\\b5G\\b", string.Empty, RegexOptions.IgnoreCase);
        name = Regex.Replace(name, "\\s+", " ").Trim();

        return string.IsNullOrWhiteSpace(name)
            ? product.Name
            : name;
    }

    private static void AddVariantLabel(List<string> parts, string? label)
    {
        var normalizedLabel = NormalizeCapacityLabel(label);
        if (string.IsNullOrWhiteSpace(normalizedLabel))
        {
            return;
        }

        var normalizedExistingText = NormalizeDisplayToken(string.Join(' ', parts));
        if (normalizedExistingText.Contains(
            NormalizeDisplayToken(normalizedLabel),
            StringComparison.Ordinal))
        {
            return;
        }

        if (!parts.Contains(normalizedLabel, StringComparer.OrdinalIgnoreCase))
        {
            parts.Add(normalizedLabel);
        }
    }

    private static string NormalizeCapacityLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        return Regex.Replace(
            label.Trim(),
            "(\\d)\\s+(GB|TB|MB)\\b",
            "$1$2",
            RegexOptions.IgnoreCase);
    }

    private static string NormalizeDisplayToken(string value)
    {
        return RemoveDiacritics(value)
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string BuildVariantDetailUrl(Product product, ProductVariant variant)
    {
        return $"/product/{Uri.EscapeDataString(product.Slug)}?variant={Uri.EscapeDataString(GetVariantKey(variant))}";
    }

    private static string GetVariantKey(ProductVariant variant)
    {
        return string.IsNullOrWhiteSpace(variant.Code)
            ? variant.Id.ToString(CultureInfo.InvariantCulture)
            : variant.Code;
    }

    private static ProductVariantImage? GetPrimaryImage(ProductVariant variant)
    {
        return variant.ProductVariantImages
            .OrderBy(image => image.Position)
            .ThenBy(image => image.Id)
            .FirstOrDefault();
    }

    private static string BuildImageAlt(
        ProductVariantImage? image,
        string detailName,
        ProductVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(image?.AltText))
        {
            return image.AltText;
        }

        return string.IsNullOrWhiteSpace(variant.ColorName)
            ? detailName
            : $"{detailName} {variant.ColorName}";
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

    private static string FormatSpecificationValue(string value, string? unit)
    {
        var trimmedValue = value.Trim();
        if (string.IsNullOrWhiteSpace(unit)
            || trimmedValue.Contains(unit, StringComparison.OrdinalIgnoreCase)
            || !Regex.IsMatch(trimmedValue, "^\\d+([\\.,]\\d+)?$"))
        {
            return trimmedValue;
        }

        return $"{trimmedValue} {unit}";
    }

    private static int GetColorOrder(ProductVariant variant)
    {
        var colorName = RemoveDiacritics(GetColorLabel(variant) ?? string.Empty).ToLowerInvariant();

        if (ContainsAny(colorName, "den", "black"))
        {
            return 0;
        }

        if (ContainsAny(colorName, "tim", "purple", "violet"))
        {
            return 1;
        }

        if (ContainsAny(colorName, "trang", "white"))
        {
            return 2;
        }

        if (ContainsAny(colorName, "xanh", "blue"))
        {
            return 3;
        }

        return 100;
    }

    private static int ParseCapacityToMb(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = Regex.Match(
            value.Replace('_', ' ').Replace('-', ' '),
            "(?<number>\\d+(?:[\\.,]\\d+)?)\\s*(?<unit>TB|GB|MB)",
            RegexOptions.IgnoreCase);

        if (!match.Success
            || !decimal.TryParse(
                match.Groups["number"].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return 0;
        }

        var multiplier = match.Groups["unit"].Value.ToUpperInvariant() switch
        {
            "TB" => 1024 * 1024,
            "GB" => 1024,
            _ => 1
        };

        return (int)Math.Round(number * multiplier, MidpointRounding.AwayFromZero);
    }

    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string Slugify(string value)
    {
        var normalizedValue = RemoveDiacritics(value).ToLowerInvariant();
        var slug = Regex.Replace(normalizedValue, "[^a-z0-9]+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? "thong-so"
            : slug;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    private readonly record struct VersionKey(
        long ProductId,
        string Signature,
        string Label,
        int RamSize,
        int StorageSize);

    private sealed record VersionGroup(
        VersionKey Key,
        Product Product,
        IReadOnlyList<ProductVariant> Variants);

    private sealed record VariantAttributeValue(
        long OptionId,
        string Code,
        string Name,
        string Value,
        string Label);

    private sealed record SpecRowDraft(
        string GroupName,
        int GroupSortOrder,
        ProductTechnicalSpecRowViewModel Row,
        int RowSortOrder);

}
