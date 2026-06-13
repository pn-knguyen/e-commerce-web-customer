using System.Linq.Expressions;
using e_commerce_web_customer.DTOs.Common;
using e_commerce_web_customer.DTOs.Product;

namespace e_commerce_web_customer.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query);

    Task<DetailProductDto?> GetByIdAsync(long id);

    Task<DetailProductDto?> GetBySlugAsync(string slug);

    Task<List<ProductDto>> FindAsync(Expression<Func<e_commerce_web_customer.Models.Product, bool>> predicate);
}
