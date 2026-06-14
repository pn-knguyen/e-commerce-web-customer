using System.Globalization;
using System.Text;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models;
using e_commerce_web_customer.ViewModels.Product;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbProductDetailDataService(EcommerceDbContext dbContext)
    : IProductDetailDataService
{
    private const string FallbackImage = "/images/logo-techstore-icon.svg";

    public async Task<ProductDetailViewModel?> CreateProductDetailAsync(
        string slug,
        long? variantId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.Category)
                .ThenInclude(category => category.CategorySpecifications)
            .Include(item => item.ProductSpecifications)
                .ThenInclude(item => item.Specification)
            .Include(item => item.ProductVariants.Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.ProductVariantImages)
            .FirstOrDefaultAsync(
                item => item.IsActive && item.Slug == slug,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var variants = product.ProductVariants
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.Id)
            .ToList();
        var selectedVariant = variants.FirstOrDefault(item => item.Id == variantId)
            ?? variants.FirstOrDefault();

        if (selectedVariant is null)
        {
            return null;
        }

        var selectedImages = selectedVariant.ProductVariantImages
            .OrderBy(item => item.Position)
            .ToList();
        var mainImage = selectedImages.FirstOrDefault();

        return new ProductDetailViewModel
        {
            Slug = product.Slug,
            SelectedVariantId = selectedVariant.Id,
            Name = product.Name,
            Brand = product.Brand.Name,
            MainImageUrl = mainImage?.ImagePath ?? FallbackImage,
            MainImageAlt = mainImage?.AltText ?? product.Name,
            CurrentPrice = selectedVariant.Price,
            Rating = product.RatingAverage,
            ReviewCount = product.RatingCount,
            Breadcrumbs =
            [
                new() { Label = "Trang chu", Url = "/" },
                new()
                {
                    Label = product.Category.Name,
                    Url = $"/catalog/{product.Category.Slug}"
                },
                new() { Label = product.Name }
            ],
            QuickLinks =
            [
                new() { Label = "Yeu thich", IconId = "product-card-icon-heart" },
                new() { Label = "Hoi dap", IconId = "hero-icon-news", Url = "#block-comment-cps" },
                new() { Label = "Thong so", IconId = "hero-icon-phone", Url = "#thong-so-ky-thuat" }
            ],
            GalleryItems = CreateGallery(product, selectedImages),
            StorageOptions = [],
            ColorOptions = variants
                .Select(variant => CreateColorOption(product, variant, selectedVariant.Id))
                .ToList(),
            TechnicalSpecSections = CreateTechnicalSpecs(product),
            RelatedProductGroups = [],
            ReviewSummary = new ProductReviewSummaryViewModel
            {
                Title = $"Danh gia {product.Name}",
                Score = product.RatingAverage,
                TotalReviews = product.RatingCount,
                RatingBreakdown = [],
                ExperienceRatings = [],
                Reviews = []
            },
            QuestionAnswerSection = new QuestionAnswerSectionViewModel
            {
                Title = $"Hoi dap ve {product.Name}",
                FormTitle = "Ban co cau hoi?",
                Description = "Gui cau hoi de duoc ho tro ve san pham.",
                Placeholder = "Nhap cau hoi cua ban",
                SubmitLabel = "Gui cau hoi",
                Threads = []
            }
        };
    }

    private static IReadOnlyList<ProductDetailGalleryItemViewModel> CreateGallery(
        Product product,
        IReadOnlyList<ProductVariantImage> images)
    {
        if (images.Count == 0)
        {
            return
            [
                new()
                {
                    Label = product.Name,
                    ImageUrl = FallbackImage,
                    ImageAlt = product.Name
                }
            ];
        }

        return images.Select((image, index) => new ProductDetailGalleryItemViewModel
        {
            Label = $"Anh {index + 1}",
            ImageUrl = image.ImagePath,
            ImageAlt = image.AltText ?? product.Name
        }).ToList();
    }

    private static ProductDetailColorOptionViewModel CreateColorOption(
        Product product,
        ProductVariant variant,
        long selectedVariantId)
    {
        var image = variant.ProductVariantImages
            .OrderBy(item => item.Position)
            .FirstOrDefault();

        return new ProductDetailColorOptionViewModel
        {
            Name = variant.ColorName ?? variant.Code,
            Url = $"/product/{product.Slug}?variantId={variant.Id}",
            ImageUrl = image?.ImagePath ?? FallbackImage,
            ImageAlt = image?.AltText ?? product.Name,
            Price = variant.Price,
            IsActive = variant.Id == selectedVariantId
        };
    }

    private static IReadOnlyList<ProductTechnicalSpecSectionViewModel> CreateTechnicalSpecs(
        Product product)
    {
        if (product.ProductSpecifications.Count == 0)
        {
            return
            [
                new()
                {
                    Id = "thong-tin-san-pham",
                    Title = "Thong tin san pham",
                    Rows =
                    [
                        new() { Label = "Thuong hieu", Value = product.Brand.Name },
                        new() { Label = "Danh muc", Value = product.Category.Name },
                        new() { Label = "Mo ta", Value = product.Description ?? "Dang cap nhat" }
                    ]
                }
            ];
        }

        var groupNames = product.Category.CategorySpecifications
            .ToDictionary(item => item.SpecificationId, item => item.GroupName);

        return product.ProductSpecifications
            .GroupBy(item =>
            {
                groupNames.TryGetValue(item.SpecificationId, out var groupName);
                return string.IsNullOrWhiteSpace(groupName)
                    ? "Thong so ky thuat"
                    : groupName;
            })
            .OrderBy(group => group.Min(item => item.SortOrder))
            .Select(group => new ProductTechnicalSpecSectionViewModel
            {
                Id = CreateSectionId(group.Key),
                Title = group.Key,
                Rows = group
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new ProductTechnicalSpecRowViewModel
                    {
                        Label = item.Specification.Name,
                        Value = string.IsNullOrWhiteSpace(item.Specification.Unit)
                            ? item.Value
                            : $"{item.Value} {item.Specification.Unit}",
                        IsHighlighted = item.IsHighlight
                    })
                    .ToList()
            })
            .ToList();
    }

    private static string CreateSectionId(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character)
                ? char.ToLowerInvariant(character)
                : '-');
        }

        return string.Join(
            '-',
            builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
