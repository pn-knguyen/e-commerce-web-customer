using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.ViewModels.Checkout;

namespace e_commerce_web_customer.Infrastructure.Orders.Mock;

public sealed class MockCheckoutPaymentMethodProvider : ICheckoutPaymentMethodProvider
{
    public Task<IReadOnlyList<CheckoutPaymentMethodViewModel>> GetActivePaymentMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var genericMethods = new List<CheckoutPaymentMethodViewModel>
        {
            new()
            {
                Id = 1,
                Name = "Thanh toán khi nhận hàng",
                Description = "Thanh toán khi nhận hàng",
                IconKey = "cod"
            },
            new()
            {
                Id = 2,
                Name = "Chuyển khoản qua ngân hàng",
                Description = "Ngân hàng nội địa",
                IconKey = "banktransfer"
            },
            new()
            {
                Id = 3,
                Name = "Thanh toán qua ví MoMo",
                Description = "Ví điện tử",
                IconKey = "momo"
            },
            new()
            {
                Id = 4,
                Name = "Thanh toán qua VNPay",
                Description = "Cổng thanh toán",
                IconKey = "vnpay"
            },
            new()
            {
                Id = 5,
                Name = "Thanh toán qua ZaloPay",
                Description = "Ví điện tử",
                IconKey = "zalopay"
            }
        };

        return Task.FromResult<IReadOnlyList<CheckoutPaymentMethodViewModel>>(genericMethods);
    }
}
