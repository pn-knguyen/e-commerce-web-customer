namespace e_commerce_web_customer.DTOs.Product;

public class DetailProductDto
{
    public long Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal RatingAverage { get; set; }

    public int RatingCount { get; set; }

    public List<DetailProductVariantDto> Variants { get; set; } = [];

    public List<DetailProductSpecificationDto> Specifications { get; set; } = [];
}

public class DetailProductVariantDto
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ColorName { get; set; }

    public bool IsDefault { get; set; }

    public List<DetailProductImageDto> Images { get; set; } = [];
}

public class DetailProductImageDto
{
    public string ImagePath { get; set; } = null!;

    public string? AltText { get; set; }

    public int Position { get; set; }
}

public class DetailProductSpecificationDto
{
    public long Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Unit { get; set; }

    public string? GroupName { get; set; }

    public int SortOrder { get; set; }

    public bool IsHighlight { get; set; }
}
