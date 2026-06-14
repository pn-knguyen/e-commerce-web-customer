using e_commerce_web_customer.DTOs;

namespace e_commerce_web_customer.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
}
