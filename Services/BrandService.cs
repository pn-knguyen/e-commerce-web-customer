using e_commerce_web_customer.DTOs;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;

namespace e_commerce_web_customer.Services;

public class BrandService : IBrandService
{
    private readonly IReadRepository<Brand> _brandRepository;

    public BrandService(IReadRepository<Brand> brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<List<BrandDto>> GetAllAsync()
    {
        var brands = await _brandRepository.GetAllAsync();

        return brands
            .Select(brand => new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name
            })
            .ToList();
    }
}
