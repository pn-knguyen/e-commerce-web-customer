namespace e_commerce_web_customer.ViewModels.Account;

public sealed class AccountOrderDetailViewModel
{
    public required AccountProfileSummaryViewModel Summary { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string OrderedDateText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string StatusTone { get; init; } = "success";
    public IReadOnlyList<AccountOrderDetailItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<AccountOrderStepViewModel> Steps { get; init; } = [];
    public required AccountOrderShipmentViewModel Shipment { get; init; }
    public required AccountOrderCustomerViewModel Customer { get; init; }
    public required AccountOrderSupportViewModel Support { get; init; }
    public required AccountOrderPaymentViewModel Payment { get; init; }
    public bool CanRetryPayment { get; init; }
    public string PaymentProviderKey { get; init; } = string.Empty;
}

public sealed class AccountOrderDetailItemViewModel
{
    public string ProductName { get; init; } = string.Empty;
    public string ProductImageUrl { get; init; } = "/images/logo-techstore-icon.svg";
    public string ProductImageAlt { get; init; } = string.Empty;
    public string UnitPriceText { get; init; } = string.Empty;
    public string ColorText { get; init; } = string.Empty;
    public string VariantText { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string LineTotalText { get; init; } = string.Empty;
}

public sealed class AccountOrderStepViewModel
{
    public string Label { get; init; } = string.Empty;
    public bool IsDone { get; init; }
}

public sealed class AccountOrderShipmentViewModel
{
    public bool HasShipment { get; init; }
    public string ProviderName { get; init; } = "Giao Hàng Nhanh";
    public string TrackingCode { get; init; } = string.Empty;
    public string StatusText { get; init; } = "Cửa hàng đang chuẩn bị hàng";
    public string StatusTone { get; init; } = "pending";
    public string UpdatedAtText { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyList<AccountOrderShipmentEventViewModel> Events { get; init; } = [];
}

public sealed class AccountOrderShipmentEventViewModel
{
    public string StatusText { get; init; } = string.Empty;
    public string StatusTone { get; init; } = "pending";
    public string OccurredAtText { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? DriverText { get; init; }
}

public sealed class AccountOrderCustomerViewModel
{
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string AddressText { get; init; } = string.Empty;
    public string NoteText { get; init; } = "-";
}

public sealed class AccountOrderSupportViewModel
{
    public string StoreAddress { get; init; } = "4/39 Quang Trung, Thới Tam Thôn, H. Hóc Môn, TP. HCM";
    public string StorePhone { get; init; } = "02871088439";
    public string ZaloUrl { get; init; } = "https://zalo.me/02871088439";
}

public sealed class AccountOrderPaymentViewModel
{
    public int ProductQuantity { get; init; }
    public string SubtotalText { get; init; } = string.Empty;
    public string DiscountText { get; init; } = string.Empty;
    public bool HasDiscount { get; init; }
    public string ShippingFeeText { get; init; } = string.Empty;
    public bool IsFreeShipping { get; init; }
    public string TotalText { get; init; } = string.Empty;
    public string PaidText { get; init; } = string.Empty;
    public string RemainingText { get; init; } = string.Empty;
}
