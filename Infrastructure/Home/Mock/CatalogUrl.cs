namespace e_commerce_web_customer.Infrastructure.Home.Mock;

internal static class CatalogUrl
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 12;

    private static readonly Dictionary<string, long> CategoryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["phone"] = 1,
        ["tablet"] = 2,
        ["laptop"] = 3,
        ["monitor"] = 4,
        ["desktop"] = 5,
        ["computer-accessories"] = 6,
        ["smartwatch"] = 7,
        ["audio"] = 8,
        ["accessories"] = 9,
        ["pc"] = 10,
        ["tv"] = 11,
        ["appliances"] = 12,
        ["tech"] = 13
    };

    private static readonly Dictionary<string, long> BrandIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apple"] = 2,
        ["macbook"] = 2,
        ["samsung"] = 3,
        ["asus"] = 3,
        ["acer"] = 4,
        ["lenovo"] = 5,
        ["msi"] = 6,
        ["hp"] = 7,
        ["dell"] = 8,
        ["lg"] = 9,
        ["xiaomi"] = 6,
        ["oppo"] = 5,
        ["sony"] = 20,
        ["intel"] = 12,
        ["amd"] = 13,
        ["nvidia"] = 14,
        ["gigabyte"] = 15,
        ["huawei"] = 16,
        ["garmin"] = 17
    };

    public static string Products(string? category = null, string? brand = null, string? name = null)
    {
        CategoryIds.TryGetValue(category ?? string.Empty, out var categoryId);
        BrandIds.TryGetValue(Normalize(brand ?? string.Empty), out var brandId);

        return ProductsById(
            categoryId == 0 ? null : categoryId,
            brandId == 0 ? null : brandId,
            name);
    }

    public static string ProductsById(long? categoryId = null, long? brandId = null, string? name = null)
    {
        var queryParts = new List<string>();

        if (categoryId.HasValue)
        {
            queryParts.Add($"CategoryId={categoryId.Value}");
        }

        if (brandId.HasValue)
        {
            queryParts.Add($"BrandId={brandId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            queryParts.Add($"Name={Uri.EscapeDataString(name)}");
        }

        queryParts.Add($"PageNumber={DefaultPageNumber}");
        queryParts.Add($"PageSize={DefaultPageSize}");

        return $"/catalog?{string.Join("&", queryParts)}";
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
