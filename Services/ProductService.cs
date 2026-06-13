using System.Linq.Expressions;
using e_commerce_web_customer.DTOs.Common;
using e_commerce_web_customer.DTOs.Product;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Services;

public class ProductService : IProductService
{
    private readonly IReadRepository<Product> _productRepository;
    private readonly IReadRepository<ProductVariant> _productVariantRepository;
    private readonly IReadRepository<ProductVariantImage> _productVariantImageRepository;

    public ProductService(
        IReadRepository<Product> productRepository,
        IReadRepository<ProductVariant> productVariantRepository,
        IReadRepository<ProductVariantImage> productVariantImageRepository)
    {
        _productRepository = productRepository;
        _productVariantRepository = productVariantRepository;
        _productVariantImageRepository = productVariantImageRepository;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 12 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;
        var variantsQuery = _productVariantRepository
            .AsQueryable()
            .Where(variant => variant.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            variantsQuery = variantsQuery
                .Where(variant => variant.Product.Name.Contains(query.Name));
        }

        if (query.CategoryId.HasValue)
        {
            variantsQuery = variantsQuery
                .Where(variant => variant.Product.CategoryId == query.CategoryId.Value);
        }

        if (query.BrandId.HasValue)
        {
            variantsQuery = variantsQuery
                .Where(variant => variant.Product.BrandId == query.BrandId.Value);
        }

        var totalItems = await variantsQuery.CountAsync();
        var productDtos = await variantsQuery
            .OrderByDescending(variant => variant.Product.CreatedAt)
            .ThenByDescending(variant => variant.IsDefault)
            .ThenBy(variant => variant.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(variant => new ProductDto
            {
                Id = variant.Product.Id,
                VariantId = variant.Id,
                Slug = variant.Product.Slug,
                ProductImage = variant.ProductVariantImages
                    .OrderBy(image => image.Position)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault(),
                Name = variant.Product.Name,
                Description = variant.Product.Description,
                Price = variant.Price,
                TotalSoldCount = variant.Product.TotalSoldCount,
                RatingCount = variant.Product.RatingCount
            })
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<DetailProductDto?> GetByIdAsync(long id)
    {
        var product = await _productRepository
            .AsQueryable()
            .Include(item => item.Brand)
            .Include(item => item.Category)
                .ThenInclude(category => category.CategorySpecifications)
            .Include(item => item.ProductSpecifications)
                .ThenInclude(productSpecification => productSpecification.Specification)
            .FirstOrDefaultAsync(item => item.Id == id);

        return product is null ? null : await MapToDetailProductDtoAsync(product);
    }

    public async Task<DetailProductDto?> GetBySlugAsync(string slug)
    {
        var product = await _productRepository
            .AsQueryable()
            .Include(item => item.Brand)
            .Include(item => item.Category)
                .ThenInclude(category => category.CategorySpecifications)
            .Include(item => item.ProductSpecifications)
                .ThenInclude(productSpecification => productSpecification.Specification)
            .FirstOrDefaultAsync(item => item.Slug == slug);

        return product is null ? null : await MapToDetailProductDtoAsync(product);
    }

    public async Task<List<ProductDto>> FindAsync(Expression<Func<Product, bool>> predicate)
    {
        var products = await _productRepository.FindAsync(predicate);

        return await MapToProductDtosAsync(products);
    }

    private async Task<List<ProductDto>> MapToProductDtosAsync(List<Product> products)
    {
        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(product => product.Id).ToList();
        var variants = await _productVariantRepository
            .AsQueryable()
            .Where(variant => productIds.Contains(variant.ProductId) && variant.IsActive)
            .ToListAsync();
        var variantIds = variants.Select(variant => variant.Id).ToList();
        var images = await _productVariantImageRepository
            .AsQueryable()
            .Where(image => variantIds.Contains(image.ProductVariantId))
            .ToListAsync();
        var variantsByProductId = variants
            .GroupBy(variant => variant.ProductId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var imagesByVariantId = images
            .GroupBy(image => image.ProductVariantId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return products.SelectMany(product =>
        {
            variantsByProductId.TryGetValue(product.Id, out var productVariants);

            return (productVariants ?? [])
                .OrderByDescending(variant => variant.IsDefault)
                .ThenBy(variant => variant.Id)
                .Select(variant => new ProductDto
                {
                    Id = product.Id,
                    VariantId = variant.Id,
                    Slug = product.Slug,
                    ProductImage = imagesByVariantId.GetValueOrDefault(variant.Id)?
                        .OrderBy(image => image.Position)
                        .FirstOrDefault()
                        ?.ImagePath,
                    Name = product.Name,
                    Description = product.Description,
                    Price = variant.Price,
                    TotalSoldCount = product.TotalSoldCount,
                    RatingCount = product.RatingCount
                });
        }).ToList();
    }

    private async Task<DetailProductDto> MapToDetailProductDtoAsync(Product product)
    {
        var variants = await _productVariantRepository
            .AsQueryable()
            .Where(variant => variant.ProductId == product.Id && variant.IsActive)
            .OrderByDescending(variant => variant.IsDefault)
            .ToListAsync();
        var variantIds = variants.Select(variant => variant.Id).ToList();
        var images = await _productVariantImageRepository
            .AsQueryable()
            .Where(image => variantIds.Contains(image.ProductVariantId))
            .OrderBy(image => image.Position)
            .ToListAsync();
        var imagesByVariantId = images
            .GroupBy(image => image.ProductVariantId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var categorySpecifications = product.Category.CategorySpecifications
            .ToDictionary(item => item.SpecificationId);

        return new DetailProductDto
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            BrandName = product.Brand.Name,
            CategoryName = product.Category.Name,
            Description = product.Description,
            RatingAverage = product.RatingAverage,
            RatingCount = product.RatingCount,
            Specifications = product.ProductSpecifications
                .OrderBy(item => item.SortOrder)
                .Select(item =>
                {
                    categorySpecifications.TryGetValue(item.SpecificationId, out var categorySpecification);

                    return new DetailProductSpecificationDto
                    {
                        Id = item.SpecificationId,
                        Key = item.Specification.Key,
                        Name = item.Specification.Name,
                        Value = item.Value,
                        Unit = item.Specification.Unit,
                        GroupName = categorySpecification?.GroupName,
                        SortOrder = item.SortOrder,
                        IsHighlight = item.IsHighlight
                    };
                })
                .ToList(),
            Variants = variants.Select(variant => new DetailProductVariantDto
            {
                Id = variant.Id,
                Code = variant.Code,
                Price = variant.Price,
                ColorName = variant.ColorName,
                IsDefault = variant.IsDefault,
                Images = imagesByVariantId.GetValueOrDefault(variant.Id)?
                    .Select(image => new DetailProductImageDto
                    {
                        ImagePath = image.ImagePath,
                        AltText = image.AltText,
                        Position = image.Position
                    })
                    .ToList() ?? []
            }).ToList()
        };
    }
}
