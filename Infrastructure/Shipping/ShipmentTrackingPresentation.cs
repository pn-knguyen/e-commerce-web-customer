namespace e_commerce_web_customer.Infrastructure.Shipping;

internal static class ShipmentTrackingPresentation
{
    public static string GetStatusText(string? status) => Normalize(status) switch
    {
        "DRAFT" => "Đang chuẩn bị thông tin giao hàng",
        "QUOTED" => "Đã tính phí vận chuyển",
        "BOOKING" => "Đang tạo vận đơn",
        "BOOKED" => "Chờ đơn vị vận chuyển lấy hàng",
        "READYTOPICK" => "Đã tạo vận đơn, chờ lấy hàng",
        "PICKINGUP" or "PICKING" => "Nhân viên đang lấy hàng",
        "MONEYCOLLECTPICKING" => "Đang thu tiền từ người gửi",
        "PICKED" => "Đơn vị vận chuyển đã lấy hàng",
        "INTRANSIT" or "TRANSPORTING" => "Đang vận chuyển",
        "STORING" => "Hàng đang ở kho GHN",
        "SORTING" => "Đang phân loại tại kho",
        "DELIVERING" => "Đang giao đến bạn",
        "MONEYCOLLECTDELIVERING" => "Đang giao hàng và thu hộ",
        "DELIVERED" => "Giao hàng thành công",
        "CANCELLED" => "Vận đơn đã hủy",
        "FAILED" => "Không thể tạo hoặc xử lý vận đơn",
        "DELIVERYFAIL" => "Giao hàng chưa thành công",
        "WAITINGTORETURN" => "Đang chờ hoàn hàng",
        "RETURN" => "Đang xử lý hoàn hàng",
        "RETURNTRANSPORTING" => "Đang vận chuyển hàng hoàn",
        "RETURNSORTING" => "Đang phân loại hàng hoàn",
        "RETURNING" => "Đang hoàn hàng về cửa hàng",
        "RETURNFAIL" => "Hoàn hàng chưa thành công",
        "RETURNED" => "Đã hoàn hàng về cửa hàng",
        "EXCEPTION" => "Đơn hàng cần được kiểm tra",
        "DAMAGE" => "Hàng hóa bị hư hỏng",
        "LOST" => "Hàng hóa bị thất lạc",
        "PROVIDERUNKNOWN" => "GHN đang cập nhật trạng thái",
        _ => "Cửa hàng đang chuẩn bị hàng"
    };

    public static string GetStatusTone(string? status) => Normalize(status) switch
    {
        "DELIVERED" => "success",
        "CANCELLED" or "FAILED" or "DELIVERYFAIL" or "RETURNFAIL"
            or "EXCEPTION" or "DAMAGE" or "LOST" => "danger",
        "WAITINGTORETURN" or "RETURN" or "RETURNTRANSPORTING"
            or "RETURNSORTING" or "RETURNING" or "RETURNED" => "return",
        "PICKED" or "INTRANSIT" or "STORING" or "TRANSPORTING"
            or "SORTING" or "DELIVERING" or "MONEYCOLLECTDELIVERING" => "shipping",
        _ => "pending"
    };

    public static int GetProgressStage(string? status) => Normalize(status) switch
    {
        "DELIVERED" => 4,
        "DELIVERING" or "MONEYCOLLECTDELIVERING" => 3,
        "PICKED" or "INTRANSIT" or "STORING" or "TRANSPORTING" or "SORTING" => 2,
        "BOOKED" or "READYTOPICK" or "PICKINGUP" or "PICKING"
            or "MONEYCOLLECTPICKING" => 1,
        _ => 0
    };

    public static string? GetSafeTrackingUrl(string? trackingUrl)
    {
        if (!Uri.TryCreate(trackingUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var isGhnHost = uri.Host.Equals("ghn.vn", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".ghn.vn", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("ghn.dev", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".ghn.dev", StringComparison.OrdinalIgnoreCase);

        return isGhnHost ? uri.AbsoluteUri : null;
    }

    private static string Normalize(string? status) =>
        string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : status.Trim()
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();
}
