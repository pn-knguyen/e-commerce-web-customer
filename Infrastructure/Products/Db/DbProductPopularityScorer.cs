using e_commerce_web_customer.Models.Entities;

namespace e_commerce_web_customer.Infrastructure.Products.Db;

internal static class DbProductPopularityScorer
{
    public static int Calculate(Product product)
    {
        var activeVariants = product.ProductVariants.Where(variant => variant.IsActive);
        return (product.IsFeatured ? 1_000_000 : 0)
            + Math.Min(activeVariants.Sum(variant => variant.SoldCount), 50_000) * 20
            + Math.Min(product.TotalSoldCount, 50_000) * 10
            + Math.Min(product.ViewsCount, 100_000)
            + (activeVariants.Any(variant => variant.Quantity > 0) ? 5_000 : 0);
    }

    public static int Calculate(Product product, ProductVariant variant)
    {
        return (product.IsFeatured ? 1_000_000 : 0)
            + Math.Min(variant.SoldCount, 50_000) * 25
            + Math.Min(product.TotalSoldCount, 50_000) * 10
            + Math.Min(product.ViewsCount, 100_000)
            + (variant.Quantity > 0 ? 5_000 : 0)
            + (variant.IsDefault ? 1_000 : 0);
    }
}
