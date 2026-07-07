namespace e_commerce_web_customer.Infrastructure.Home.Content;

internal static class HomeAccessoryDirectoryContent
{
    public const string Title = "Sắm thêm phụ kiện chất lượng";
    public const string MockViewAllUrl = "/catalog?cat=accessories";

    public static IReadOnlyList<HomeAccessoryCategoryDefinition> Categories { get; } =
    [
        new("Phụ kiện Apple", "apple", "apple-accessories", "phu-kien-apple"),
        new("Cáp, sạc", "charging", "charging-cables", "cap-sac"),
        new("Pin sạc dự phòng", "power-bank", "power-banks", "pin-du-phong"),
        new("Ốp lưng - Bao da", "cases", "phone-cases", "op-lung"),
        new("Dán màn hình", "screen-protector", "screen-protectors", "dan-man-hinh"),
        new("Thẻ nhớ, USB", "memory-usb", "memory-usb", "the-nho-usb"),
        new("Gaming Gear, Playstation", "gaming", "gaming-playstation", "ban-phim"),
        new("Sim 4G - 5G", "sim", "sim-4g-5g", "bo-phat-wifi-4g"),
        new("Thiết bị mạng", "network", "network-devices", "thiet-bi-mang"),
        new("Camera", "security-camera", "security-camera", "camera"),
        new("Gimbal", "gimbal", "gimbal", "gimbal"),
        new("Flycam", "drone", "drone", "flycam"),
        new("Máy ảnh", "camera", "cameras", "may-anh"),
        new("Chuột, bàn phím", "keyboard-mouse", "keyboard-mouse", "chuot"),
        new("Balo, túi xách", "backpack", "backpacks", "tui-chong-soc"),
        new("Hub chuyển đổi", "usb-c-hub", "usb-c-hubs", "hub-switch"),
        new("Phụ kiện điện thoại", "phone-accessories", "phone-pouches", "phu-kien-di-dong"),
        new("Phụ kiện Laptop", "laptop-accessories", "laptop-accessories", "phu-kien-laptop")
    ];

    public static string GetMockImageUrl(HomeAccessoryCategoryDefinition category)
    {
        return $"/images/categories/accessories/{category.MockImageName}.webp";
    }
}

internal sealed record HomeAccessoryCategoryDefinition(
    string MockLabel,
    string MockTypeSlug,
    string MockImageName,
    string DbCategorySlug);
