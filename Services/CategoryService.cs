using e_commerce_web_customer.DTOs;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;

namespace e_commerce_web_customer.Services;

public class CategoryService : ICategoryService
{
    private readonly IReadRepository<Category> _categoryRepository;

    public CategoryService(IReadRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToList();
    }
}
