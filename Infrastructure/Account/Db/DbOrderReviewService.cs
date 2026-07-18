using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Account.Db;

public sealed class DbOrderReviewService(EcommerceDbContext dbContext) : IOrderReviewService
{
    private const int MaxCommentLength = 1000;

    public async Task<OrderReviewResult> SubmitReviewAsync(
        string? customerEmail,
        OrderReviewInput input,
        CancellationToken cancellationToken = default)
    {
        var email = customerEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Fail("Vui lòng đăng nhập để gửi đánh giá.");
        }

        if (input.Stars is < 1 or > 5)
        {
            return Fail("Vui lòng chọn số sao từ 1 đến 5.");
        }

        var comment = NormalizeComment(input.Comment);
        if (comment?.Length > MaxCommentLength)
        {
            return Fail($"Nội dung đánh giá tối đa {MaxCommentLength:N0} ký tự.");
        }

        var orderItem = await dbContext.OrderItems
            .Include(item => item.Order)
                .ThenInclude(order => order!.User)
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant!.Product)
            .Include(item => item.Rating)
            .FirstOrDefaultAsync(item => item.Id == input.OrderItemId, cancellationToken);

        if (orderItem?.Order?.User is null || orderItem.ProductVariant?.Product is null)
        {
            return Fail("Không tìm thấy sản phẩm cần đánh giá trong đơn hàng.");
        }

        if (!string.Equals(orderItem.Order.User.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return Fail("Bạn không có quyền đánh giá sản phẩm trong đơn hàng này.");
        }

        if (orderItem.Order.PaymentStatus != PaymentStatus.Paid
            || orderItem.Order.OrderStatus != OrderStatus.Completed)
        {
            return Fail("Bạn chỉ có thể đánh giá sau khi đơn hàng đã thanh toán và nhận hàng thành công.");
        }

        var now = DateTime.UtcNow;
        if (orderItem.Rating is null)
        {
            dbContext.Ratings.Add(new Rating
            {
                OrderItemId = orderItem.Id,
                UserId = orderItem.Order.UserId,
                Stars = input.Stars,
                Comment = comment,
                IsApproved = true,
                CreatedAt = now
            });
        }
        else
        {
            orderItem.Rating.Stars = input.Stars;
            orderItem.Rating.Comment = comment;
            orderItem.Rating.IsApproved = true;
            orderItem.Rating.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RefreshProductRatingAsync(orderItem.ProductVariant.Product, cancellationToken);

        return new OrderReviewResult(true, "Cảm ơn bạn, đánh giá đã được lưu.");
    }

    private async Task RefreshProductRatingAsync(Product product, CancellationToken cancellationToken)
    {
        var summary = await dbContext.Ratings
            .AsNoTracking()
            .Where(rating => rating.IsApproved)
            .Where(rating => rating.OrderItem != null
                && rating.OrderItem.ProductVariant != null
                && rating.OrderItem.ProductVariant.ProductId == product.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = group.Average(rating => (decimal)rating.Stars)
            })
            .FirstOrDefaultAsync(cancellationToken);

        product.RatingCount = summary?.Count ?? 0;
        product.RatingAverage = summary is null
            ? 0m
            : decimal.Round(summary.Average, 2, MidpointRounding.AwayFromZero);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeComment(string? comment)
    {
        var trimmed = comment?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static OrderReviewResult Fail(string message) => new(false, message);
}
