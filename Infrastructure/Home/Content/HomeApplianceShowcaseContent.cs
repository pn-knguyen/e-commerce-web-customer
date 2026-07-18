using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Home.Content;

internal static class HomeApplianceShowcaseContent
{
    public const string Id = "home-appliance-showcase";
    public const string Title = "ĐỒ GIA DỤNG";
    public const string RootCategorySlug = "do-gia-dung";
    public const string FallbackImageUrl = "/images/logo-techstore-icon.svg";
    public const int VisibleItemCount = 11;

    public static IReadOnlyList<HomeApplianceShowcaseGroupDefinition> Groups { get; } =
    [
        new(
            "appliance-home",
            "Thiết bị gia đình",
            "/images/banner_do_gia_dung_home/giadung-594x95-t5.webp",
            ["thiet-bi-gia-dinh", "gia-dung-nha-bep"]),
        new(
            "appliance-health",
            "Sức khỏe - Làm đẹp",
            "/images/banner_do_gia_dung_home/skld-block-2026.webp",
            ["suc-khoe-lam-dep"])
    ];

    public static HomeApplianceShowcaseViewModel CreateMock()
    {
        var headerLinks = MockBrands
            .Select(brand => new CategoryDirectoryLinkViewModel
            {
                Label = brand.Name,
                Url = $"{BuildCatalogUrl(RootCategorySlug)}&brand={Uri.EscapeDataString(brand.Slug)}"
            })
            .ToList();

        return new HomeApplianceShowcaseViewModel
        {
            Id = Id,
            Title = Title,
            ViewAllUrl = BuildCatalogUrl(RootCategorySlug),
            HeaderLinks = headerLinks,
            Columns = Groups
                .Select(group => CreateMockColumn(group))
                .Where(column => column.Items.Count > 0)
                .ToList()
        };
    }

    public static string BuildCatalogUrl(string categorySlug)
    {
        return $"/catalog?cat={Uri.EscapeDataString(categorySlug)}";
    }

    private static HomeApplianceShowcaseColumnViewModel CreateMockColumn(
        HomeApplianceShowcaseGroupDefinition group)
    {
        var items = MockItems
            .Where(item => group.SectionSlugs.Contains(item.ParentSlug, StringComparer.OrdinalIgnoreCase))
            .Select(item => new CategoryDirectoryItemViewModel
            {
                Label = item.Label,
                Url = BuildCatalogUrl(item.Slug),
                ImageUrl = FallbackImageUrl,
                ImageAlt = item.Label
            })
            .Take(VisibleItemCount)
            .ToList();

        var firstSectionSlug = group.SectionSlugs.FirstOrDefault() ?? RootCategorySlug;

        return new HomeApplianceShowcaseColumnViewModel
        {
            Id = group.Id,
            Title = group.Title,
            ViewAllUrl = BuildCatalogUrl(firstSectionSlug),
            BannerUrl = BuildCatalogUrl(firstSectionSlug),
            BannerImageUrl = group.BannerImageUrl,
            BannerImageAlt = group.Title,
            Items = items
        };
    }

    private static IReadOnlyList<MockApplianceBrand> MockBrands { get; } =
    [
        new("Roborock", "roborock"),
        new("Xiaomi", "xiaomi"),
        new("Dreame", "dreame"),
        new("Tineco", "tineco"),
        new("Sharp", "sharp")
    ];

    private static IReadOnlyList<MockApplianceItem> MockItems { get; } =
    [
        new("Robot hút bụi", "robot-hut-bui", "thiet-bi-gia-dinh"),
        new("Máy hút bụi cầm tay", "may-hut-bui-cam-tay", "thiet-bi-gia-dinh"),
        new("Máy lọc không khí", "may-loc-khong-khi", "thiet-bi-gia-dinh"),
        new("Quạt", "quat", "thiet-bi-gia-dinh"),
        new("Máy chiếu", "may-chieu", "thiet-bi-gia-dinh"),
        new("Bàn ủi", "ban-ui", "thiet-bi-gia-dinh"),
        new("Máy hút ẩm", "may-hut-am", "thiet-bi-gia-dinh"),
        new("Máy sưởi", "may-suoi", "thiet-bi-gia-dinh"),
        new("Nồi chiên không dầu", "noi-chien-khong-dau", "gia-dung-nha-bep"),
        new("Nồi cơm điện", "noi-com-dien", "gia-dung-nha-bep"),
        new("Máy làm sữa hạt", "may-lam-sua-hat", "gia-dung-nha-bep"),
        new("Máy ép trái cây", "may-ep-trai-cay", "gia-dung-nha-bep"),
        new("Cân sức khỏe", "can-suc-khoe", "suc-khoe-lam-dep"),
        new("Máy sấy tóc", "may-say-toc", "suc-khoe-lam-dep"),
        new("Bàn chải điện", "ban-chai-dien", "suc-khoe-lam-dep"),
        new("Máy tăm nước", "may-tam-nuoc", "suc-khoe-lam-dep"),
        new("Máy cạo râu", "may-cao-rau", "suc-khoe-lam-dep"),
        new("Máy massage", "may-massage", "suc-khoe-lam-dep")
    ];

    private sealed record MockApplianceBrand(string Name, string Slug);

    private sealed record MockApplianceItem(string Label, string Slug, string ParentSlug);
}

internal sealed record HomeApplianceShowcaseGroupDefinition(
    string Id,
    string Title,
    string BannerImageUrl,
    IReadOnlyList<string> SectionSlugs);
