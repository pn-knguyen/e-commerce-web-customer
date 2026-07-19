using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Navigation.Mock;

internal static class MockSiteCategoryMegaMenuData
{
    private const string ComputingImageRoot = "/images/products/computing";

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Phone { get; } =
    [
        Group("Thương hiệu", "/catalog?cat=phone&brand=",
            "Apple", "Samsung", "Xiaomi", "OPPO", "HONOR", "Sony", "Nokia", "realme"),
        Group("Điện thoại nổi bật", "/catalog?cat=phone&q=",
            "iPhone 17 Pro Max", "Galaxy S26 Ultra", "OPPO Find X9 Ultra", "Xiaomi 17T", "HONOR 600 5G"),
        Group("Máy tính bảng", "/catalog?cat=tablet&q=",
            "iPad Pro", "iPad Air", "Galaxy Tab S11", "Xiaomi Pad 7", "Lenovo Legion Tab"),
        Group("Khoảng giá", "/catalog?cat=phone&price=",
            "Dưới 2 triệu", "Từ 2 - 5 triệu", "Từ 5 - 10 triệu", "Từ 10 - 20 triệu", "Trên 20 triệu")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Laptop { get; } =
    [
        WithGroupClass(
            Group("Thương hiệu", "/catalog?cat=laptop&brand=",
                "MacBook", "ASUS", "Lenovo", "Dell", "HP", "Acer", "MSI", "Gigabyte", "Samsung"),
            "site-category-mega-group--laptop site-category-mega-group--laptop-brands"),
        WithGroupClass(
            Group("Nhu cầu sử dụng", "/catalog?cat=laptop&usage=",
                "Văn phòng", "Gaming", "Mỏng nhẹ", "Đồ họa - kỹ thuật", "Sinh viên", "Cảm ứng", "Laptop AI"),
            "site-category-mega-group--laptop site-category-mega-group--laptop-usage"),
        WithGroupClass(
            FilterGroup("Dòng chip", "laptop", "chip",
                ("Laptop Core i3", "laptop-core-i3"),
                ("Laptop Core i5", "laptop-core-i5"),
                ("Laptop Core i7", "laptop-core-i7"),
                ("Laptop Core i9", "laptop-core-i9"),
                ("Intel Core Ultra", "intel-core-ultra"),
                ("Apple M1 Series", "apple-m1-series"),
                ("Apple M2 Series", "apple-m2-series"),
                ("Apple M3 Series", "apple-m3-series"),
                ("Apple M4 Series", "apple-m4-series"),
                ("Apple M5 Series", "apple-m5-series"),
                ("AMD Ryzen", "amd-ryzen")),
            "site-category-mega-group--laptop site-category-mega-group--laptop-chip"),
        WithGroupClass(
            FilterGroup("Kích thước màn hình", "laptop", "screen-size",
                ("Laptop 13 inch", "13"),
                ("Laptop 14 inch", "14"),
                ("Laptop 15.6 inch", "15.6"),
                ("Laptop 16 inch", "16")),
            "site-category-mega-group--laptop site-category-mega-group--laptop-screen"),
        WithGroupClass(
            PriceGroup("Phân khúc giá", "laptop",
                ("Dưới 10 triệu", "under-10m"),
                ("Từ 10 - 15 triệu", "10m-15m"),
                ("Từ 15 - 20 triệu", "15m-20m"),
                ("Từ 20 - 30 triệu", "20m-30m"),
                ("Trên 30 triệu", "over-30m")),
            "site-category-mega-group--laptop site-category-mega-group--laptop-price")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Audio { get; } =
    [
        WithGroupClass(
            Group("Chọn loại tai nghe", "/catalog?cat=audio&type=",
                "Bluetooth", "Chụp tai", "Nhét tai", "Có dây", "Thể thao", "Gaming"),
            "site-category-mega-group--audio site-category-mega-group--audio-headphones"),
        WithGroupClass(
            Group("Loa", "/catalog?cat=speaker&type=",
                "Loa Bluetooth", "Loa Soundbar", "Loa Karaoke", "Loa vi tính", "Loa Sub", "Loa trợ giảng"),
            "site-category-mega-group--audio site-category-mega-group--audio-speakers"),
        WithGroupClass(
            Group("Mic thu âm", "/catalog?cat=microphone&type=",
                "Mic cài áo", "Mic podcast", "Mic livestream", "Micro không dây", "Mic karaoke"),
            "site-category-mega-group--audio site-category-mega-group--audio-microphones"),
        WithGroupClass(
            Group("Hãng tai nghe", "/catalog?cat=audio&brand=",
                "AirPods", "Sony", "JBL", "Samsung", "Marshall", "Bose", "Edifier", "Xiaomi", "Anker"),
            "site-category-mega-group--audio site-category-mega-group--audio-headphone-brands"),
        WithGroupClass(
            Group("Hãng loa", "/catalog?cat=speaker&brand=",
                "JBL", "Marshall", "Harman Kardon", "Samsung", "Sony", "LG", "Tronsmart"),
            "site-category-mega-group--audio site-category-mega-group--audio-speaker-brands")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Watch { get; } =
    [
        Group("Đồng hồ thông minh", "/catalog?cat=watch&brand=",
            "Apple Watch", "Samsung", "Garmin", "Huawei", "Xiaomi", "Amazfit"),
        Group("Nhu cầu sử dụng", "/catalog?cat=watch&usage=",
            "Thể thao", "Sức khỏe", "Trẻ em", "Thời trang", "Nghe gọi độc lập"),
        Group("Camera", "/catalog?cat=camera&type=",
            "Camera an ninh", "Camera hành trình", "Webcam", "Camera 360", "Camera ngoài trời"),
        Group("Thiết bị quay", "/catalog?cat=camera-accessories&type=",
            "Gimbal", "Flycam", "Action camera", "Tripod", "Phụ kiện máy ảnh")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Appliances { get; } =
    [
        Group("Gia dụng nhà bếp", "/catalog?cat=appliances&type=",
            "Nồi chiên không dầu", "Nồi cơm điện", "Máy xay", "Bếp điện", "Máy lọc nước"),
        Group("Chăm sóc nhà cửa", "/catalog?cat=appliances&type=",
            "Robot hút bụi", "Máy hút bụi", "Máy lọc không khí", "Quạt", "Máy hút ẩm"),
        Group("Làm đẹp", "/catalog?cat=beauty&type=",
            "Máy sấy tóc", "Máy tạo kiểu tóc", "Máy cạo râu", "Bàn chải điện", "Máy rửa mặt"),
        Group("Chăm sóc sức khỏe", "/catalog?cat=health&type=",
            "Máy massage", "Cân sức khỏe", "Máy đo huyết áp", "Máy tăm nước")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Accessories { get; } =
    [
        Group("Phụ kiện điện thoại", "/catalog?cat=accessories&type=",
            "Ốp lưng", "Dán màn hình", "Cáp sạc", "Củ sạc", "Pin dự phòng"),
        Group("Phụ kiện Apple", "/catalog?cat=accessories&brand=",
            "AirTag", "Apple Pencil", "Magic Keyboard", "MagSafe", "AirPods"),
        Group("Lưu trữ và kết nối", "/catalog?cat=accessories&type=",
            "Thẻ nhớ", "USB", "Hub chuyển đổi", "Thiết bị mạng", "Ổ cứng di động"),
        Group("Phụ kiện khác", "/catalog?cat=accessories&type=",
            "Balo - túi xách", "Gaming Gear", "Giá đỡ", "Gimbal", "Phụ kiện laptop")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> ComputerAccessories { get; } =
    [
        ImageGroup(
            "Danh mục phụ kiện máy tính",
            "site-category-mega-group--computer site-category-mega-group--computer-categories",
            ("CPU", "/catalog?cat=computer-accessories&type=cpu", "component-10.webp"),
            ("Mainboard", "/catalog?cat=computer-accessories&type=mainboard", "component-03.webp"),
            ("RAM", "/catalog?cat=computer-accessories&type=ram", "component-09.webp"),
            ("Ổ cứng", "/catalog?cat=computer-accessories&type=storage", "component-01.webp"),
            ("Card màn hình", "/catalog?cat=computer-accessories&type=gpu", "component-05.webp"),
            ("Nguồn máy tính", "/catalog?cat=computer-accessories&type=psu", "component-10.webp"),
            ("Case máy tính", "/catalog?cat=computer-accessories&type=case", "desktop-06.webp")),
        LinkGroup(
            "Thương hiệu",
            "site-category-mega-group--computer site-category-mega-group--computer-brands",
            ("ASUS", "/catalog?cat=computer-accessories&brand=asus"),
            ("Intel", "/catalog?cat=computer-accessories&brand=intel"),
            ("MSI", "/catalog?cat=computer-accessories&brand=msi"),
            ("Samsung", "/catalog?cat=computer-accessories&brand=samsung"),
            ("Gigabyte", "/catalog?cat=computer-accessories&brand=gigabyte"),
            ("ASRock", "/catalog?cat=computer-accessories&brand=asrock")),
        PriceGroup(
            "Mức giá",
            "computer-accessories",
            ("Dưới 1 triệu", "under-1m"),
            ("Từ 1 - 3 triệu", "1m-3m"),
            ("Từ 3 - 5 triệu", "3m-5m"),
            ("Từ 5 - 10 triệu", "5m-10m"),
            ("Từ 10 - 20 triệu", "10m-20m"),
            ("Trên 20 triệu", "over-20m"))
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Pc { get; } =
    [
        ImageGroup(
            "Loại PC",
            "site-category-mega-group--computer site-category-mega-group--computer-categories",
            ("PC Gaming", "/catalog?cat=desktop&type=gaming", "desktop-01.webp"),
            ("PC Văn phòng", "/catalog?cat=desktop&type=office", "desktop-04.webp"),
            ("PC All In One", "/catalog?cat=desktop&type=all-in-one", "desktop-10.webp"),
            ("PC AI", "/catalog?cat=desktop&type=ai", "desktop-06.webp")),
        ImageGroup(
            "Chọn màn hình theo nhu cầu",
            "site-category-mega-group--computer site-category-mega-group--computer-categories",
            ("Gaming", "/catalog?cat=monitor&usage=gaming", "monitor-01.webp"),
            ("Văn phòng", "/catalog?cat=monitor&usage=office", "monitor-02.webp"),
            ("Đồ họa", "/catalog?cat=monitor&usage=graphics", "monitor-07.webp"),
            ("Màn hình cong", "/catalog?cat=monitor&design=curved", "monitor-03.webp"),
            ("Màn hình lập trình", "/catalog?cat=monitor&usage=coding", "monitor-09.webp"),
            ("Màn hình di động", "/catalog?cat=monitor&design=portable", "monitor-04.webp")),
        LinkGroup(
            "Thương hiệu PC",
            "site-category-mega-group--computer site-category-mega-group--computer-brands",
            ("AMD", "/catalog?cat=desktop&brand=amd"),
            ("ASUS", "/catalog?cat=desktop&brand=asus"),
            ("Intel", "/catalog?cat=desktop&brand=intel"),
            ("MSI", "/catalog?cat=desktop&brand=msi"),
            ("Gigabyte", "/catalog?cat=desktop&brand=gigabyte"),
            ("Rosa", "/catalog?cat=desktop&brand=rosa")),
        LinkGroup(
            "Thương hiệu màn hình",
            "site-category-mega-group--computer site-category-mega-group--computer-brands",
            ("AOC", "/catalog?cat=monitor&brand=aoc"),
            ("ASUS", "/catalog?cat=monitor&brand=asus"),
            ("Dell", "/catalog?cat=monitor&brand=dell"),
            ("LG", "/catalog?cat=monitor&brand=lg"),
            ("MSI", "/catalog?cat=monitor&brand=msi"),
            ("Samsung", "/catalog?cat=monitor&brand=samsung")),
        LinkGroup(
            "Thương hiệu máy in",
            "site-category-mega-group--computer site-category-mega-group--computer-brands",
            ("Brother", "/catalog?cat=printer&brand=brother"),
            ("Canon", "/catalog?cat=printer&brand=canon"),
            ("HP", "/catalog?cat=printer&brand=hp"),
            ("HPRT", "/catalog?cat=printer&brand=hprt")),
        PriceGroup(
            "Mức giá PC",
            "desktop",
            ("Dưới 10 triệu", "under-10"),
            ("Từ 10 - 20 triệu", "10-20"),
            ("Từ 20 - 30 triệu", "20-30"),
            ("Trên 30 triệu", "over-30")),
        PriceGroup(
            "Mức giá màn hình",
            "monitor",
            ("Dưới 1 triệu", "under-1m"),
            ("Từ 1 - 3 triệu", "1m-3m"),
            ("Từ 3 - 5 triệu", "3m-5m"),
            ("Từ 5 - 10 triệu", "5m-10m"),
            ("Trên 10 triệu", "over-10m")),
        PriceGroup(
            "Mức giá máy in",
            "printer",
            ("Dưới 1 triệu", "under-1m"),
            ("Từ 1 - 3 triệu", "1m-3m"),
            ("Từ 3 - 5 triệu", "3m-5m"),
            ("Từ 5 - 10 triệu", "5m-10m"),
            ("Trên 10 triệu", "over-10m"))
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Tv { get; } =
    [
        WithGroupClass(
            Group("Tivi", "/catalog?cat=tv&type=",
                "Smart Tivi", "Google Tivi", "Tivi OLED", "Tivi Mini LED", "Tivi 4K"),
            "site-category-mega-group--electronics site-category-mega-group--electronics-tv"),
        WithGroupClass(
            Group("Điện lạnh", "/catalog?cat=home-electronics&type=",
                "Máy lạnh", "Tủ lạnh", "Máy giặt", "Máy sấy", "Tủ đông"),
            "site-category-mega-group--electronics site-category-mega-group--electronics-cooling"),
        WithGroupClass(
            Group("Thiết bị giải trí", "/catalog?cat=entertainment&type=",
                "Android TV Box", "Máy chiếu", "Loa thanh", "Điều khiển Tivi"),
            "site-category-mega-group--electronics site-category-mega-group--electronics-entertainment"),
        WithGroupClass(
            Group("Thương hiệu Tivi", "/catalog?cat=tv&brand=",
                "Samsung", "LG", "Sony", "TCL", "Xiaomi", "Hisense"),
            "site-category-mega-group--electronics site-category-mega-group--electronics-brands"),
        WithGroupClass(
            FilterGroup("Mức giá Tivi", "tv", "price",
                ("Dưới 5 triệu", "under-5m"),
                ("Từ 5 - 10 triệu", "5m-10m"),
                ("Từ 10 - 20 triệu", "10m-20m"),
                ("Trên 20 triệu", "over-20m")),
            "site-category-mega-group--electronics site-category-mega-group--electronics-price"),
        WithGroupClass(
            FilterGroup("Mức giá điện máy", "home-electronics", "price",
                ("Dưới 5 triệu", "under-5m"),
                ("Từ 5 - 10 triệu", "5m-10m"),
                ("Từ 10 - 20 triệu", "10m-20m"),
                ("Trên 20 triệu", "over-20m")),
            "site-category-mega-group--electronics site-category-mega-group--electronics-price")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> TradeIn { get; } =
    [
        Group("Thu cũ theo sản phẩm", "/catalog?cat=trade-in&type=",
            "Điện thoại", "Máy tính bảng", "Laptop", "Đồng hồ", "Tai nghe"),
        Group("Nâng cấp thiết bị", "/catalog?cat=trade-in&upgrade=",
            "Lên đời iPhone", "Lên đời Samsung", "Đổi laptop mới", "Đổi máy tính bảng"),
        Group("Quy trình", "/catalog?cat=trade-in&step=",
            "Định giá sản phẩm", "Kiểm tra thiết bị", "Nhận trợ giá", "Hoàn tất đổi máy"),
        Group("Thông tin hỗ trợ", "/catalog?cat=trade-in&help=",
            "Bảng giá thu cũ", "Điều kiện thu cũ", "Cửa hàng áp dụng", "Câu hỏi thường gặp")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Used { get; } =
    [
        Group("Sản phẩm đã qua sử dụng", "/catalog?cat=used&type=",
            "Điện thoại cũ", "Laptop cũ", "Tablet cũ", "Đồng hồ cũ", "Phụ kiện cũ"),
        Group("Tình trạng sản phẩm", "/catalog?cat=used&condition=",
            "Đẹp như mới", "Trầy xước nhẹ", "Đã kích hoạt", "Hàng trưng bày"),
        Group("Khoảng giá", "/catalog?cat=used&price=",
            "Dưới 5 triệu", "Từ 5 - 10 triệu", "Từ 10 - 20 triệu", "Trên 20 triệu"),
        Group("Chính sách", "/catalog?cat=used&policy=",
            "Bảo hành hàng cũ", "Đổi trả 30 ngày", "Kiểm định chất lượng", "Trả góp")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Deals { get; } =
    [
        Group("Khuyến mãi nổi bật", "/catalog?cat=deals&campaign=",
            "Giảm giá cuối tuần", "Ưu đãi thành viên", "Flash Sale", "Freeship", "Trả góp 0%"),
        Group("Ưu đãi theo ngành hàng", "/catalog?cat=deals&category=",
            "Điện thoại", "Laptop", "Phụ kiện", "Âm thanh", "Đồng hồ", "Gia dụng"),
        Group("Chương trình đặc biệt", "/catalog?cat=deals&program=",
            "S-Student", "Thu cũ đổi mới", "Ưu đãi doanh nghiệp", "Combo tiết kiệm"),
        Group("Mã giảm giá", "/catalog?cat=deals&voucher=",
            "Voucher thanh toán", "Voucher ứng dụng", "Ưu đãi ngân hàng", "Mã freeship")
    ];

    public static IReadOnlyList<SiteCategoryMenuGroupViewModel> Tech { get; } =
    [
        Group("Chủ đề công nghệ", "/catalog?cat=tech&topic=",
            "Tin mới", "Đánh giá sản phẩm", "So sánh", "Tư vấn mua sắm", "Thủ thuật"),
        Group("Sản phẩm", "/catalog?cat=tech&product=",
            "Điện thoại", "Laptop", "Máy tính bảng", "Đồng hồ", "Âm thanh"),
        Group("Hướng dẫn", "/catalog?cat=tech&type=",
            "Cài đặt thiết bị", "Sao lưu dữ liệu", "Bảo mật", "Tối ưu hiệu năng"),
        Group("Xu hướng", "/catalog?cat=tech&trend=",
            "AI", "Thiết bị gập", "Gaming", "Smart Home", "Năng lượng xanh")
    ];

    private static SiteCategoryMenuGroupViewModel Group(
        string title,
        string urlPrefix,
        params string[] labels)
    {
        return new SiteCategoryMenuGroupViewModel
        {
            Title = title,
            Links = labels
                .Select(label => new SiteCategoryMenuLinkViewModel
                {
                    Label = label,
                    Url = $"{urlPrefix}{Uri.EscapeDataString(label.ToLowerInvariant())}"
            })
            .ToArray()
        };
    }

    private static SiteCategoryMenuGroupViewModel ImageGroup(
        string title,
        string cssClass,
        params (string Label, string Url, string ImageName)[] links)
    {
        return new SiteCategoryMenuGroupViewModel
        {
            Title = title,
            CssClass = cssClass,
            Links = links
                .Select(link => new SiteCategoryMenuLinkViewModel
                {
                    Label = link.Label,
                    Url = link.Url,
                    ImageUrl = $"{ComputingImageRoot}/{link.ImageName}",
                    ImageAlt = link.Label
                })
                .ToArray()
        };
    }

    private static SiteCategoryMenuGroupViewModel LinkGroup(
        string title,
        string cssClass,
        params (string Label, string Url)[] links)
    {
        return new SiteCategoryMenuGroupViewModel
        {
            Title = title,
            CssClass = cssClass,
            Links = links
                .Select(link => new SiteCategoryMenuLinkViewModel
                {
                    Label = link.Label,
                    Url = link.Url
                })
                .ToArray()
        };
    }

    private static SiteCategoryMenuGroupViewModel FilterGroup(
        string title,
        string category,
        string filterKey,
        params (string Label, string Value)[] links)
    {
        return LinkGroup(
            title,
            string.Empty,
            links
                .Select(link => (
                    link.Label,
                    $"/catalog?cat={Uri.EscapeDataString(category)}&f_{Uri.EscapeDataString(filterKey)}={Uri.EscapeDataString(link.Value)}"))
                .ToArray());
    }

    private static SiteCategoryMenuGroupViewModel WithGroupClass(
        SiteCategoryMenuGroupViewModel group,
        string cssClass)
    {
        return new SiteCategoryMenuGroupViewModel
        {
            Title = group.Title,
            Links = group.Links,
            CssClass = string.IsNullOrWhiteSpace(group.CssClass)
                ? cssClass
                : $"{group.CssClass} {cssClass}"
        };
    }

    private static SiteCategoryMenuGroupViewModel PriceGroup(
        string title,
        string category,
        params (string Label, string Value)[] links)
    {
        return LinkGroup(
            title,
            "site-category-mega-group--computer site-category-mega-group--computer-price",
            links
                .Select(link => (
                    link.Label,
                    $"/catalog?cat={Uri.EscapeDataString(category)}&f_price={Uri.EscapeDataString(link.Value)}"))
                .ToArray());
    }
}
