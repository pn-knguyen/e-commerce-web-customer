using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Navigation;

public sealed class MockSiteCategoryMenuProvider : ISiteCategoryMenuProvider
{
    private static readonly SiteCategoryMenuViewModel Menu = new()
    {
        Items =
        [
            Item("site-cat-phone", "/catalog?cat=phone", "Điện thoại, Tablet", "phone"),
            Item("site-cat-laptop", "/catalog?cat=laptop", "Laptop", "laptop"),
            Item("site-cat-audio", "/catalog?cat=audio", "Âm thanh, Mic thu âm", "audio"),
            Item("site-cat-watch", "/catalog?cat=watch", "Đồng hồ, Camera", "watch"),
            Item("site-cat-appliances", "/catalog?cat=appliances", "Đồ gia dụng, Làm đẹp", "home"),
            Item("site-cat-accessories", "/catalog?cat=accessories", "Phụ kiện", "cable"),
            Item("site-cat-pc", "/catalog?cat=pc", "PC, Màn hình, Máy in", "desktop"),
            Item("site-cat-tv", "/catalog?cat=tv", "Tivi, Điện máy", "tv"),
            Item("site-cat-tradein", "/trade-in", "Thu cũ đổi mới", "swap"),
            Item("site-cat-used", "/catalog?tag=used", "Hàng cũ", "history"),
            Item("site-cat-deals", "/deals", "Khuyến mãi", "discount", true),
            Item("site-cat-tech", "/catalog?cat=tech", "Tin công nghệ", "news")
        ]
    };

    public SiteCategoryMenuViewModel GetMenu() => Menu;

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
