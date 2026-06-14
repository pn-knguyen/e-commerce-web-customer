namespace e_commerce_web_customer.DTOs.Product;

public class ProductQuery
{
    public string? Name { get; set; }

    public long? CategoryId { get; set; }

    public long? BrandId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}
