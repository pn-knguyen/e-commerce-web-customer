using e_commerce_web_customer.DTOs;

namespace e_commerce_web_customer.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync();
}
