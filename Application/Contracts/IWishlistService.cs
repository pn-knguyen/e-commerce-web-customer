namespace e_commerce_web_customer.Application.Contracts;

public interface IWishlistService
{
    Task<WishlistToggleResult> ToggleAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default);

    Task<WishlistActionResult> RemoveAsync(
        string userEmail,
        string productVariantKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, bool>> GetStatusesAsync(
        string userEmail,
        IReadOnlyCollection<string> productVariantKeys,
        CancellationToken cancellationToken = default);
}

public sealed record WishlistToggleResult(
    bool Success,
    bool IsWishlisted,
    string Message);

public sealed record WishlistActionResult(
    bool Success,
    string Message);
