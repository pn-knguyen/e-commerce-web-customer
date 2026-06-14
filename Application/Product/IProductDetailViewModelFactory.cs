using e_commerce_web_customer.ViewModels.Product;

namespace e_commerce_web_customer.Application.Product;

public interface IProductDetailViewModelFactory
{
    Task<ProductDetailViewModel?> CreateAsync(
        string slug,
        long? variantId = null,
        CancellationToken cancellationToken = default);
}
