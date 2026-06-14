using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.ViewModels.Product;

namespace e_commerce_web_customer.Application.Product;

public sealed class DbProductDetailViewModelFactory(
    IProductDetailDataService productDetailDataService) : IProductDetailViewModelFactory
{
    public Task<ProductDetailViewModel?> CreateAsync(
        string slug,
        long? variantId = null,
        CancellationToken cancellationToken = default)
    {
        return productDetailDataService.CreateProductDetailAsync(
            slug,
            variantId,
            cancellationToken);
    }
}
