using e_commerce_web_customer.Application.Account;

namespace e_commerce_web_customer.Infrastructure.Account.Mock;

public sealed class MockOrderReviewService : IOrderReviewService
{
    public Task<OrderReviewResult> SubmitReviewAsync(
        string? customerEmail,
        OrderReviewInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (input.Stars is < 1 or > 5)
        {
            return Task.FromResult(new OrderReviewResult(false, "Vui lòng chọn số sao từ 1 đến 5."));
        }

        return Task.FromResult(new OrderReviewResult(true, "Đã ghi nhận đánh giá mẫu."));
    }
}
