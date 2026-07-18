using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Home.Content;

internal static class HomeTvCategorySectionContent
{
    private const string Category = "tv";
    private const string ImageRoot = "/images/products/computing";

    public static IReadOnlyList<CategoryQuickLinkViewModel> CreateQuickLinks()
    {
        return
        [
            QuickLink("32 inch", "f_screen-size=32", "monitor-01.webp"),
            QuickLink("43 inch", "f_screen-size=43", "monitor-02.webp"),
            QuickLink("55 inch", "f_screen-size=55", "monitor-03.webp"),
            QuickLink("60 inch", "f_screen-size=60", "monitor-04.webp"),
            QuickLink("65 inch", "f_screen-size=65", "monitor-05.webp"),
            QuickLink("75 inch", "f_screen-size=75", "monitor-06.webp")
        ];
    }

    public static IReadOnlyList<CategoryBrandViewModel> CreateBrands()
    {
        return Brands("Samsung", "Xiaomi", "Coocaa", "LG", "Sony", "TCL", "AQUA");
    }

    public static IReadOnlyList<ProductCardViewModel> CreateProducts()
    {
        return
        [
            Product(
                "tv-lg-43ua8055psa",
                "Smart Tivi LG UHD 4K 43 inch 2026 (43UA8055PSA)",
                "monitor-01.webp",
                "8.690.000đ",
                "10.690.000đ",
                "Giảm 19%",
                "Smember giảm đến 87.000đ",
                "Tặng khung treo trị giá 400K khi mua tivi với khách hàng mới..."),
            Product(
                "tv-samsung-32t4202",
                "Smart Tivi Samsung HD 32 inch T4202",
                "monitor-02.webp",
                "4.990.000đ",
                "6.490.000đ",
                "Giảm 23%",
                "Smember giảm đến 50.000đ",
                "Tặng khung treo trị giá 400K khi mua tivi với khách hàng mới..."),
            Product(
                "tv-lg-55ua8055psa",
                "Smart Tivi LG UHD 4K 55 inch 2026 (55UA8055PSA)",
                "monitor-03.webp",
                "10.900.000đ",
                "15.590.000đ",
                "Giảm 30%",
                "Smember giảm đến 109.000đ",
                "Tặng khung treo trị giá 400K khi mua tivi với khách hàng mới..."),
            Product(
                "tv-lg-50ua8055psa",
                "Smart tivi LG UHD 4K 50 inch 2026 (50UA8055PSA)",
                "monitor-04.webp",
                "9.990.000đ",
                "13.590.000đ",
                "Giảm 26%",
                "Smember giảm đến 100.000đ",
                "Tặng khung treo trị giá 400K khi mua tivi với khách hàng mới..."),
            Product(
                "tv-lg-65ua8055psa",
                "Smart Tivi LG UHD 4K 65 inch 2026 (65UA8055PSA)",
                "monitor-05.webp",
                "13.190.000đ",
                "19.490.000đ",
                "Giảm 32%",
                "Smember giảm đến 132.000đ",
                "Tặng khung treo trị giá 400K khi mua tivi với khách hàng mới..."),
            Product(
                "tv-samsung-55du8000",
                "Smart Tivi Samsung Crystal UHD 4K 55 inch DU8000",
                "monitor-06.webp",
                "11.490.000đ",
                "14.990.000đ",
                "Giảm 23%",
                "Smember giảm đến 115.000đ",
                "Miễn phí giao lắp và hỗ trợ trả góp 0%."),
            Product(
                "tv-xiaomi-a-pro-65",
                "Smart Tivi Xiaomi A Pro QLED 4K 65 inch",
                "monitor-07.webp",
                "12.990.000đ",
                "16.990.000đ",
                "Giảm 24%",
                "Smember giảm đến 130.000đ",
                "Tặng gói bảo hành mở rộng khi mua online."),
            Product(
                "tv-sony-bravia-60",
                "Google Tivi Sony Bravia 4K 60 inch XR-60X90L",
                "monitor-08.webp",
                "18.990.000đ",
                "23.990.000đ",
                "Giảm 21%",
                "Smember giảm đến 190.000đ",
                "Hỗ trợ lắp đặt tận nơi trong ngày."),
            Product(
                "tv-tcl-qled-65",
                "Google Tivi TCL QLED 4K 65 inch C655",
                "monitor-09.webp",
                "10.990.000đ",
                "14.490.000đ",
                "Giảm 24%",
                "Smember giảm đến 110.000đ",
                "Tặng voucher phụ kiện khi mua kèm giá treo."),
            Product(
                "tv-aqua-75-aqt75s800ug",
                "Google Tivi AQUA 4K 75 inch AQT75S800UG",
                "monitor-10.webp",
                "19.990.000đ",
                "25.990.000đ",
                "Giảm 23%",
                "Smember giảm đến 200.000đ",
                "Ưu đãi giao lắp cho khách hàng mới.")
        ];
    }

    private static CategoryQuickLinkViewModel QuickLink(
        string label,
        string query,
        string imageName)
    {
        return new CategoryQuickLinkViewModel
        {
            Label = label,
            Url = $"/catalog?cat={Category}&{query}",
            ImageUrl = $"{ImageRoot}/{imageName}"
        };
    }

    private static IReadOnlyList<CategoryBrandViewModel> Brands(params string[] labels)
    {
        return labels
            .Select(label => new CategoryBrandViewModel
            {
                Label = label,
                Url = $"/catalog?cat={Category}&brand={Uri.EscapeDataString(label.ToLowerInvariant())}"
            })
            .ToArray();
    }

    private static ProductCardViewModel Product(
        string id,
        string name,
        string imageName,
        string price,
        string oldPrice,
        string discount,
        string memberOffer,
        string note)
    {
        return new ProductCardViewModel
        {
            Id = id,
            Name = name,
            Url = $"/product/{id}",
            ImageUrl = $"{ImageRoot}/{imageName}",
            ImageAlt = name,
            CurrentPrice = price,
            OldPrice = oldPrice,
            DiscountLabel = discount,
            InstallmentLabel = "Trả góp 0%",
            MemberOffer = memberOffer,
            PromotionNote = note,
            DeliveryLabel = "Giao 2 giờ",
            Location = "Hồ Chí Minh",
            Rating = 5.0m
        };
    }
}
