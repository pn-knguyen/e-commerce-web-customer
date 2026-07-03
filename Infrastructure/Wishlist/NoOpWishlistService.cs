using e_commerce_web_customer.Application.Contracts;

namespace e_commerce_web_customer.Infrastructure.Wishlist;

public sealed class NoOpWishlistService : IWishlistService
{
    public Task<WishlistToggleResult> ToggleAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WishlistToggleResult(
            false,
            false,
            "Tính năng yêu thích không khả dụng trong chế độ dữ liệu mẫu."));
    }

    public Task<WishlistActionResult> RemoveAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WishlistActionResult(
            false,
            "Tính năng yêu thích không khả dụng trong chế độ dữ liệu mẫu."));
    }

    public Task<IReadOnlyDictionary<string, bool>> GetStatusesAsync(
        string userEmail,
        IReadOnlyCollection<string> productVariantKeys,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<string, bool>>(new Dictionary<string, bool>());
    }
}
