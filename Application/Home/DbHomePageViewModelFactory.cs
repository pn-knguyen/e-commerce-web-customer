using System.Globalization;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Home;

public sealed class DbHomePageViewModelFactory : IHomePageViewModelFactory
{
    private readonly ISiteCategoryMenuProvider _categoryMenuProvider;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public DbHomePageViewModelFactory(
        ISiteCategoryMenuProvider categoryMenuProvider,
        ICategoryService categoryService,
        IProductService productService)
    {
        _categoryMenuProvider = categoryMenuProvider;
        _categoryService = categoryService;
        _productService = productService;
    }

    public async Task<HomeIndexViewModel> CreateAsync()
    {
        var phoneSection = await GetPhoneSectionAsync();

        return new HomeIndexViewModel
        {
            Hero = HomeHeroViewModelFactory.Create(_categoryMenuProvider.GetMenu().Items),
            FeaturedCategorySections = [phoneSection],
            AccessoryDirectory = AccessoryDirectoryFactory.Create(),
            AdditionalCategorySections =
            [
                ComputerCategorySectionFactory.Create(),
                AudioWearablesCategorySectionFactory.Create()
            ]
        };
    }

    private async Task<CategoryProductsViewModel> GetPhoneSectionAsync()
    {
        var categories = await _categoryService.GetAllAsync();
        var phoneCategory = categories.FirstOrDefault(category =>
            category.Name.Contains("điện thoại", StringComparison.OrdinalIgnoreCase));

        if (phoneCategory is null)
        {
            return PhoneCategorySectionFactory.Create([]);
        }

        var products = await _productService.GetAllAsync(new ProductQuery
        {
            CategoryId = phoneCategory.Id,
            PageNumber = 1,
            PageSize = 20
        });

        return PhoneCategorySectionFactory.Create(
            products.Items.Select(MapToProductCard).ToList(),
            phoneCategory.Id);
    }

    private static ProductCardViewModel MapToProductCard(ProductDto product)
    {
        return new ProductCardViewModel
        {
            Id = product.VariantId.ToString(CultureInfo.InvariantCulture),
            Name = product.Name,
            Url = $"/product/{product.Slug}?variantId={product.VariantId}",
            ImageUrl = string.IsNullOrWhiteSpace(product.ProductImage)
                ? "/images/logo-techstore-icon.svg"
                : product.ProductImage,
            ImageAlt = product.Name,
            CurrentPrice = product.Price.HasValue
                ? $"{product.Price.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}đ"
                : "Liên hệ",
            PromotionNote = product.Description,
            DeliveryLabel = "Giao 2 giờ",
            Location = "Hồ Chí Minh",
            RatingCount = product.RatingCount,
            ShowWishlistAction = true
        };
    }
}
