using e_commerce_web_customer.ViewModels.Cart;
using e_commerce_web_customer.ViewModels.Checkout;

namespace e_commerce_web_customer.Application.Contracts;

public interface ICartDemoDataProvider
{
    Task<IReadOnlyList<CartItemViewModel>> GetCartItemsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckoutItemViewModel>> GetCheckoutItemsAsync(
        CancellationToken cancellationToken = default);
}
