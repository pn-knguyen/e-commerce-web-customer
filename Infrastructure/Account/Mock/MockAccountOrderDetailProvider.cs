using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.ViewModels.Account;

namespace e_commerce_web_customer.Infrastructure.Account.Mock;

public sealed class MockAccountOrderDetailProvider : IAccountOrderDetailProvider
{
    public Task<AccountOrderDetailViewModel?> GetOrderDetailAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = orderCode.Trim().TrimStart('#');
        var model = CreateOrders()
            .FirstOrDefault(item => item.OrderCode.TrimStart('#').Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(model);
    }

    private static IReadOnlyList<AccountOrderDetailViewModel> CreateOrders() =>
    [
        new()
        {
            Summary = CreateSummary(),
            OrderCode = "#WN0303995253",
            OrderedDateText = "20/12/2025",
            StatusText = "Đã nhận hàng",
            StatusTone = "success",
            Items =
            [
                new()
                {
                    ProductName = "USB 3.2 KINGSTON DATATRAVELER EXODIA DTXM 128GB",
                    ProductImageUrl = "/images/categories/accessories/memory-usb.webp",
                    ProductImageAlt = "USB Kingston DataTraveler Exodia 128GB",
                    UnitPriceText = "349.000đ",
                    ColorText = "Đen",
                    VariantText = "128GB - Đen",
                    Quantity = 1,
                    LineTotalText = "349.000đ"
                },
                new()
                {
                    ProductName = "USB 3.2 KINGSTON DATATRAVELER EXODIA DTXM 64GB",
                    ProductImageUrl = "/images/categories/accessories/memory-usb.webp",
                    ProductImageAlt = "USB Kingston DataTraveler Exodia 64GB",
                    UnitPriceText = "219.000đ",
                    ColorText = "Đen",
                    VariantText = "64GB - Đen",
                    Quantity = 1,
                    LineTotalText = "219.000đ"
                }
            ],
            Steps =
            [
                new() { Label = "Đặt hàng thành công", IsDone = true },
                new() { Label = "Đã chuẩn bị hàng", IsDone = true },
                new() { Label = "Đang vận chuyển", IsDone = true },
                new() { Label = "Đã nhận hàng", IsDone = true }
            ],
            Shipment = new AccountOrderShipmentViewModel
            {
                HasShipment = true,
                ProviderName = "Giao Hàng Nhanh (GHN)",
                TrackingCode = "LXRU49",
                StatusText = "Giao hàng thành công",
                StatusTone = "success",
                UpdatedAtText = "20/12/2025 15:42",
                TrackingUrl = "https://tracking.ghn.dev/?order_code=LXRU49",
                Events =
                [
                    new()
                    {
                        StatusText = "Giao hàng thành công",
                        StatusTone = "success",
                        OccurredAtText = "20/12/2025 15:42"
                    },
                    new()
                    {
                        StatusText = "Đang giao đến bạn",
                        StatusTone = "shipping",
                        OccurredAtText = "20/12/2025 13:15"
                    },
                    new()
                    {
                        StatusText = "Đã tạo vận đơn, chờ lấy hàng",
                        StatusTone = "pending",
                        OccurredAtText = "20/12/2025 09:10"
                    }
                ]
            },
            Customer = new AccountOrderCustomerViewModel
            {
                FullName = "Phạm Ngọc Khôi Nguyên",
                PhoneNumber = "0815020970",
                AddressText = "4/39 Quang Trung, Thới Tam Thôn, H. Hóc Môn, TP. HCM",
                NoteText = "-"
            },
            Support = new AccountOrderSupportViewModel(),
            Payment = new AccountOrderPaymentViewModel
            {
                ProductQuantity = 2,
                SubtotalText = "568.000đ",
                DiscountText = "-191.000đ",
                HasDiscount = true,
                ShippingFeeText = "Miễn phí",
                IsFreeShipping = true,
                TotalText = "377.000đ",
                PaidText = "377.000đ",
                RemainingText = "0đ"
            }
        },
        new()
        {
            Summary = CreateSummary(),
            OrderCode = "#WN0303995250",
            OrderedDateText = "20/12/2025",
            StatusText = "Chờ xác nhận",
            StatusTone = "pending",
            Items =
            [
                new()
                {
                    ProductName = "USB 3.2 Kingston DataTraveler Exodia DTX 128GB-Đen",
                    ProductImageUrl = "/images/categories/accessories/memory-usb.webp",
                    ProductImageAlt = "USB Kingston DataTraveler Exodia màu đen",
                    UnitPriceText = "349.000đ",
                    ColorText = "Đen",
                    VariantText = "128GB - Đen",
                    Quantity = 1,
                    LineTotalText = "349.000đ"
                }
            ],
            Steps =
            [
                new() { Label = "Đặt hàng thành công", IsDone = true },
                new() { Label = "Đã chuẩn bị hàng", IsDone = false },
                new() { Label = "Đang vận chuyển", IsDone = false },
                new() { Label = "Đã nhận hàng", IsDone = false }
            ],
            Shipment = new AccountOrderShipmentViewModel
            {
                HasShipment = false,
                UpdatedAtText = "Thông tin vận chuyển sẽ xuất hiện sau khi cửa hàng tạo vận đơn."
            },
            Customer = new AccountOrderCustomerViewModel
            {
                FullName = "Phạm Ngọc Khôi Nguyên",
                PhoneNumber = "0815020970",
                AddressText = "4/39 Quang Trung, Thới Tam Thôn, H. Hóc Môn, TP. HCM",
                NoteText = "-"
            },
            Support = new AccountOrderSupportViewModel(),
            Payment = new AccountOrderPaymentViewModel
            {
                ProductQuantity = 1,
                SubtotalText = "349.000đ",
                DiscountText = "0đ",
                HasDiscount = false,
                ShippingFeeText = "30.000đ",
                IsFreeShipping = false,
                TotalText = "379.000đ",
                PaidText = "0đ",
                RemainingText = "379.000đ"
            }
        }
    ];

    private static AccountProfileSummaryViewModel CreateSummary() => new()
    {
        FullName = "Phạm Ngọc Khôi Nguyên",
        Email = "demo@techstore.vn",
        PhoneNumber = "0815020970",
        MaskedPhoneNumber = "081*****70",
        GenderText = "Nam",
        PasswordUpdatedAtText = "12/11/2023 12:42",
        OrderCountText = "34",
        TotalSpentText = "9.829.000đ"
    };
}
