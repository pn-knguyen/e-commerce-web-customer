using e_commerce_web_customer.ViewModels.Checkout;

namespace e_commerce_web_customer.Application.Contracts;

public interface ICheckoutVoucherService
{
    Task<IReadOnlyList<CheckoutVoucherViewModel>> GetAvailableVouchersAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default);

    Task<CheckoutVoucherSelectionResult> GetBestVoucherAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default);

    Task<CheckoutVoucherValidationResult> ValidateAsync(
        long? voucherId,
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default);
}

public sealed record CheckoutVoucherValidationResult(
    bool IsValid,
    long? VoucherId,
    decimal DiscountAmount,
    string? ErrorMessage);

public sealed record CheckoutVoucherSelectionResult(
    long? VoucherId,
    decimal DiscountAmount);
