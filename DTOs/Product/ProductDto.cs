namespace e_commerce_web_customer.DTOs.Product;

public class ProductDto
{
    public long Id { get; set; }

    public long VariantId { get; set; }

    public string Slug { get; set; } = null!;

    public string? ProductImage { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public int TotalSoldCount { get; set; }

    public int RatingCount { get; set; }
}
