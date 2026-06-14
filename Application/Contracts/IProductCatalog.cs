using e_commerce_web_customer.Application.Products;

namespace e_commerce_web_customer.Application.Contracts;

public interface IProductCatalog
{
    Task<IReadOnlyList<ProductReadModel>> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default);

    Task<ProductReadModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}
