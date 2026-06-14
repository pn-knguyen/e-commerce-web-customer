using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.ViewModels.Catalog;

namespace e_commerce_web_customer.Application.Catalog;

public interface ICatalogPageViewModelFactory
{
    Task<CatalogIndexViewModel> CreateAsync(ProductQuery query);
}
