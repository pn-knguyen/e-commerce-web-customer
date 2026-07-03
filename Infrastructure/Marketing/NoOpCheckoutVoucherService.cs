using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.ViewModels.Checkout;

namespace e_commerce_web_customer.Infrastructure.Marketing;

public sealed class NoOpCheckoutVoucherService : ICheckoutVoucherService
{
    public Task<IReadOnlyList<CheckoutVoucherViewModel>> GetAvailableVouchersAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CheckoutVoucherViewModel>>([]);
    }

    public Task<CheckoutVoucherSelectionResult> GetBestVoucherAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CheckoutVoucherSelectionResult(null, 0m));
    }

    public Task<CheckoutVoucherValidationResult> ValidateAsync(
        long? voucherId,
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(voucherId is null
            ? new CheckoutVoucherValidationResult(true, null, 0m, null)
            : new CheckoutVoucherValidationResult(false, null, 0m, "Voucher không khả dụng trong chế độ dữ liệu mẫu."));
    }
}
