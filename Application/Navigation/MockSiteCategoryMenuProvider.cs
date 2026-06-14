using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Navigation;

public sealed class MockSiteCategoryMenuProvider : ISiteCategoryMenuProvider
{
    private static readonly SiteCategoryMenuViewModel Menu = new()
    {
        Items =
        [
            Item("site-cat-phone", Catalog("phone"), "Điện thoại, Tablet", "phone"),
            Item("site-cat-laptop", Catalog("laptop"), "Laptop", "laptop"),
            Item("site-cat-audio", Catalog("audio"), "Âm thanh, Mic thu âm", "audio"),
            Item("site-cat-watch", Catalog("smartwatch"), "Đồng hồ, Camera", "watch"),
            Item("site-cat-appliances", Catalog("appliances"), "Đồ gia dụng, Làm đẹp", "home"),
            Item("site-cat-accessories", Catalog("accessories"), "Phụ kiện", "cable"),
            Item("site-cat-pc", Catalog("pc"), "PC, Màn hình, Máy in", "desktop"),
            Item("site-cat-tv", Catalog("tv"), "Tivi, Điện máy", "tv"),
            Item("site-cat-tradein", "/trade-in", "Thu cũ đổi mới", "swap"),
            Item("site-cat-used", Catalog(name: "Hàng cũ"), "Hàng cũ", "history"),
            Item("site-cat-deals", "/deals", "Khuyến mãi", "discount", true),
            Item("site-cat-tech", Catalog("tech"), "Tin công nghệ", "news")
        ]
    };

    public SiteCategoryMenuViewModel GetMenu() => Menu;

    private static string Catalog(string? category = null, string? brand = null, string? name = null)
    {
        return e_commerce_web_customer.Application.Home.CatalogUrl.Products(category, brand, name);
    }

    private static SiteCategoryMenuItemViewModel Item(
        string id,
        string url,
        string label,
        string icon,
        bool isHighlighted = false)
    {
        return new SiteCategoryMenuItemViewModel
        {
            Id = id,
            Url = url,
            Label = label,
            Icon = icon,
            IsHighlighted = isHighlighted
        };
    }
}
