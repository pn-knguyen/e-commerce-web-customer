namespace e_commerce_web_customer.Application.Account;

public interface IOrderReviewService
{
    Task<OrderReviewResult> SubmitReviewAsync(
        string? customerEmail,
        OrderReviewInput input,
        CancellationToken cancellationToken = default);
}

public sealed record OrderReviewInput(
    long OrderItemId,
    int Stars,
    string? Comment);

public sealed record OrderReviewResult(
    bool Success,
    string Message);
