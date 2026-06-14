using e_commerce_web_customer.Application.Product;
using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Product;
using System.Globalization;
using System.Text;

namespace e_commerce_web_customer.Infrastructure.Products.Db;

public sealed class DbProductDetailViewModelFactory : IProductDetailViewModelFactory
{
    private const string FallbackImage = "/images/logo-techstore-icon.svg";
    private readonly IProductService _productService;

    public DbProductDetailViewModelFactory(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<ProductDetailViewModel?> CreateAsync(string slug, long? variantId = null)
    {
        var product = await _productService.GetBySlugAsync(slug);

        return product is null ? null : MapToViewModel(product, variantId);
    }

    private static ProductDetailViewModel MapToViewModel(DetailProductDto product, long? variantId)
    {
        var variants = product.Variants.Count > 0
            ? product.Variants
            : [new DetailProductVariantDto { Code = product.Slug, Price = 0, IsDefault = true }];
        var defaultVariant = variants.FirstOrDefault(variant => variant.Id == variantId)
            ?? variants.FirstOrDefault(variant => variant.IsDefault)
            ?? variants[0];
        var selectedVariantImages = defaultVariant.Images
            .OrderBy(image => image.Position)
            .ToList();
        var mainImage = selectedVariantImages.FirstOrDefault();
        var productName = string.IsNullOrWhiteSpace(defaultVariant.ColorName)
            ? product.Name
            : $"{product.Name} {defaultVariant.ColorName}";

        return new ProductDetailViewModel
        {
            Slug = product.Slug,
            SelectedVariantId = defaultVariant.Id > 0 ? defaultVariant.Id : null,
            Name = productName,
            Brand = product.BrandName,
            MainImageUrl = mainImage?.ImagePath ?? FallbackImage,
            MainImageAlt = mainImage?.AltText ?? product.Name,
            CurrentPrice = defaultVariant.Price,
            Rating = product.RatingAverage,
            ReviewCount = product.RatingCount,
            Breadcrumbs =
            [
                new() { Label = "Trang chủ", Url = "/" },
                new() { Label = product.CategoryName, Url = "/catalog" },
                new() { Label = product.Name }
            ],
            QuickLinks =
            [
                new() { Label = "Yêu thích", IconId = "product-card-icon-heart" },
                new() { Label = "Hỏi đáp", IconId = "hero-icon-news", Url = "#block-comment-cps" },
                new() { Label = "Thông số", IconId = "hero-icon-phone" }
            ],
            GalleryItems = CreateGallery(product, selectedVariantImages),
            StorageOptions = [],
            ColorOptions = variants.Select(variant =>
            {
                var image = variant.Images.OrderBy(item => item.Position).FirstOrDefault();

                return new ProductDetailColorOptionViewModel
                {
                    Name = variant.ColorName ?? variant.Code,
                    Url = $"/product/{product.Slug}?variantId={variant.Id}",
                    ImageUrl = image?.ImagePath ?? FallbackImage,
                    ImageAlt = image?.AltText ?? product.Name,
                    Price = variant.Price,
                    IsActive = variant.Id == defaultVariant.Id
                };
            }).ToList(),
            TechnicalSpecSections = CreateTechnicalSpecSections(product),
            RelatedProductGroups = [],
            ReviewSummary = new ProductReviewSummaryViewModel
            {
                Title = $"Đánh giá {product.Name}",
                Score = product.RatingAverage,
                TotalReviews = product.RatingCount,
                RatingBreakdown = [],
                ExperienceRatings = [],
                Reviews = []
            },
            QuestionAnswerSection = new ProductQuestionAnswerSectionViewModel
            {
                Title = $"Hỏi đáp về {product.Name}",
                FormTitle = "Bạn có câu hỏi?",
                Description = "Gửi câu hỏi để được hỗ trợ về sản phẩm.",
                Placeholder = "Nhập câu hỏi của bạn",
                SubmitLabel = "Gửi câu hỏi",
                Threads = []
            }
        };
    }

    private static IReadOnlyList<ProductDetailGalleryItemViewModel> CreateGallery(
        DetailProductDto product,
        List<DetailProductImageDto> images)
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
            Label = $"Ảnh {index + 1}",
            ImageUrl = image.ImagePath,
            ImageAlt = image.AltText ?? product.Name
        }).ToList();
    }

    private static IReadOnlyList<ProductTechnicalSpecSectionViewModel> CreateTechnicalSpecSections(
        DetailProductDto product)
    {
        if (product.Specifications.Count == 0)
        {
            return
            [
                new()
                {
                    Id = "thong-tin-san-pham",
                    Title = "Thông tin sản phẩm",
                    Rows =
                    [
                        new() { Label = "Thương hiệu", Value = product.BrandName },
                        new() { Label = "Danh mục", Value = product.CategoryName },
                        new() { Label = "Mô tả", Value = product.Description ?? "Đang cập nhật" }
                    ]
                }
            ];
        }

        return product.Specifications
            .GroupBy(specification => string.IsNullOrWhiteSpace(specification.GroupName)
                ? "Thông số kỹ thuật"
                : specification.GroupName)
            .OrderBy(group => group.Min(specification => specification.SortOrder))
            .Select(group => new ProductTechnicalSpecSectionViewModel
            {
                Id = CreateSectionId(group.Key),
                Title = group.Key,
                Rows = group
                    .OrderBy(specification => specification.SortOrder)
                    .Select(specification => new ProductTechnicalSpecRowViewModel
                    {
                        Label = specification.Name,
                        Value = FormatSpecificationValue(specification),
                        IsHighlighted = specification.IsHighlight
                    })
                    .ToList()
            })
            .ToList();
    }

    private static string FormatSpecificationValue(DetailProductSpecificationDto specification)
    {
        return string.IsNullOrWhiteSpace(specification.Unit)
            ? specification.Value
            : $"{specification.Value} {specification.Unit}";
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
