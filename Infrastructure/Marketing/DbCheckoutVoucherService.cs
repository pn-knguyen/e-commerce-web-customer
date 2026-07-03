using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using e_commerce_web_customer.ViewModels.Checkout;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Marketing;

public sealed class DbCheckoutVoucherService(EcommerceDbContext dbContext)
    : ICheckoutVoucherService
{
    public async Task<IReadOnlyList<CheckoutVoucherViewModel>> GetAvailableVouchersAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserAsync(userEmail, cancellationToken);

        var vouchers = await dbContext.Vouchers
            .AsNoTracking()
            .Include(voucher => voucher.VoucherUsers)
            .Include(voucher => voucher.VoucherUsages)
            .Include(voucher => voucher.VoucherTargets)
            .Where(voucher => voucher.IsActive)
            .OrderByDescending(voucher => voucher.Priority)
            .ThenBy(voucher => voucher.EndDate)
            .ToListAsync(cancellationToken);

        var cartTargets = await BuildCartTargetsAsync(items, cancellationToken);

        return vouchers.Select(voucher =>
            {
                var validation = ValidateVoucher(voucher, user?.Id, subtotal, cartTargets);
                var discount = validation.IsValid
                    ? CalculateDiscount(voucher, subtotal)
                    : 0m;

                return new CheckoutVoucherViewModel
                {
                    Id = voucher.Id,
                    Code = voucher.Code,
                    Description = voucher.Description,
                    DiscountType = voucher.DiscountType.ToString(),
                    DiscountValue = voucher.DiscountValue,
                    MinOrderValue = voucher.MinOrderValue,
                    MaxDiscountValue = voucher.MaxDiscountValue,
                    EndDate = voucher.EndDate,
                    Priority = voucher.Priority,
                    IsAvailable = validation.IsValid,
                    UnavailableReason = validation.ErrorMessage,
                    DiscountAmount = discount
                };
            })
            .ToList();
    }

    public async Task<CheckoutVoucherSelectionResult> GetBestVoucherAsync(
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        var vouchers = await GetAvailableVouchersAsync(
            userEmail,
            items,
            subtotal,
            cancellationToken);

        var bestVoucher = vouchers
            .Where(voucher => voucher.IsAvailable && voucher.DiscountAmount > 0)
            .OrderByDescending(voucher => voucher.DiscountAmount)
            .ThenByDescending(voucher => voucher.Priority)
            .ThenBy(voucher => voucher.EndDate)
            .FirstOrDefault();

        return bestVoucher is null
            ? new CheckoutVoucherSelectionResult(null, 0m)
            : new CheckoutVoucherSelectionResult(bestVoucher.Id, bestVoucher.DiscountAmount);
    }

    public async Task<CheckoutVoucherValidationResult> ValidateAsync(
        long? voucherId,
        string userEmail,
        IReadOnlyList<CheckoutItemViewModel> items,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        if (voucherId is null)
        {
            return new CheckoutVoucherValidationResult(true, null, 0m, null);
        }

        var user = await ResolveUserAsync(userEmail, cancellationToken);
        if (user is null)
        {
            return Invalid("Không xác định được tài khoản để áp dụng voucher.");
        }

        var voucher = await dbContext.Vouchers
            .Include(item => item.VoucherUsers)
            .Include(item => item.VoucherUsages)
            .Include(item => item.VoucherTargets)
            .FirstOrDefaultAsync(item => item.Id == voucherId.Value, cancellationToken);

        if (voucher is null)
        {
            return Invalid("Voucher không tồn tại hoặc đã bị gỡ bỏ.");
        }

        var cartTargets = await BuildCartTargetsAsync(items, cancellationToken);
        var validation = ValidateVoucher(voucher, user.Id, subtotal, cartTargets);
        if (!validation.IsValid)
        {
            return validation;
        }

        return new CheckoutVoucherValidationResult(
            true,
            voucher.Id,
            CalculateDiscount(voucher, subtotal),
            null);
    }

    private static CheckoutVoucherValidationResult ValidateVoucher(
        Voucher voucher,
        long? userId,
        decimal subtotal,
        CartVoucherTargets cartTargets)
    {
        var now = DateTime.UtcNow;

        if (!voucher.IsActive)
        {
            return Invalid("Voucher hiện không còn hoạt động.");
        }

        if (voucher.StartDate > now || voucher.EndDate < now)
        {
            return Invalid("Voucher đã hết hạn hoặc chưa đến thời gian sử dụng.");
        }

        if (subtotal < voucher.MinOrderValue)
        {
            return Invalid($"Đơn hàng cần tối thiểu {CheckoutViewModel.FormatPrice(voucher.MinOrderValue)} để dùng voucher này.");
        }

        if (voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value)
        {
            return Invalid("Voucher đã hết lượt sử dụng.");
        }

        if (userId is null
            && (voucher.MaxUsesPerUser.HasValue || voucher.VoucherUsers.Count > 0))
        {
            return Invalid("Không xác định được tài khoản để kiểm tra lượt dùng voucher.");
        }

        var userUsedCount = userId.HasValue
            ? voucher.VoucherUsages.Count(item => item.UserId == userId.Value)
            : 0;
        if (voucher.MaxUsesPerUser.HasValue && userUsedCount >= voucher.MaxUsesPerUser.Value)
        {
            return Invalid("Bạn đã dùng hết lượt cho voucher này.");
        }

        if (voucher.VoucherUsers.Count > 0)
        {
            var assignedVoucher = voucher.VoucherUsers.FirstOrDefault(item => item.UserId == userId!.Value);
            if (assignedVoucher is null)
            {
                return Invalid("Voucher này chỉ dành cho một số khách hàng nhất định.");
            }

            if (assignedVoucher.UsedCount >= assignedVoucher.MaxUses)
            {
                return Invalid("Bạn đã dùng hết lượt được cấp cho voucher này.");
            }
        }

        if (voucher.VoucherTargets.Count > 0 && !TargetsCartItem(voucher.VoucherTargets, cartTargets))
        {
            return Invalid("Voucher không áp dụng cho sản phẩm trong giỏ hàng.");
        }

        return new CheckoutVoucherValidationResult(true, voucher.Id, 0m, null);
    }

    private static bool TargetsCartItem(
        IEnumerable<VoucherTarget> voucherTargets,
        CartVoucherTargets cartTargets)
    {
        return voucherTargets.Any(target => target.TargetType switch
        {
            TargetType.Product => cartTargets.ProductIds.Contains(target.TargetId),
            TargetType.ProductVariant => cartTargets.VariantIds.Contains(target.TargetId),
            TargetType.Category => cartTargets.CategoryIds.Contains(target.TargetId),
            TargetType.Brand => cartTargets.BrandIds.Contains(target.TargetId),
            _ => false
        });
    }

    private async Task<CartVoucherTargets> BuildCartTargetsAsync(
        IReadOnlyList<CheckoutItemViewModel> items,
        CancellationToken cancellationToken)
    {
        var variantIds = items
            .Select(item => long.TryParse(item.ProductId, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
        var variantCodes = items
            .Where(item => !long.TryParse(item.ProductId, out _))
            .Select(item => item.ProductId.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (variantIds.Count == 0 && variantCodes.Count == 0)
        {
            return new CartVoucherTargets([], [], [], []);
        }

        var variants = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Product)
            .Where(variant =>
                variantIds.Contains(variant.Id)
                || variantCodes.Contains(variant.Code))
            .Select(variant => new
            {
                VariantId = variant.Id,
                variant.ProductId,
                CategoryId = variant.Product != null ? variant.Product.CategoryId : 0,
                BrandId = variant.Product != null ? variant.Product.BrandId : 0
            })
            .ToListAsync(cancellationToken);

        return new CartVoucherTargets(
            variants.Select(item => item.VariantId).ToHashSet(),
            variants.Select(item => item.ProductId).ToHashSet(),
            variants.Where(item => item.CategoryId > 0).Select(item => item.CategoryId).ToHashSet(),
            variants.Where(item => item.BrandId > 0).Select(item => item.BrandId).ToHashSet());
    }

    private static decimal CalculateDiscount(Voucher voucher, decimal subtotal)
    {
        var discount = voucher.DiscountType switch
        {
            DiscountType.Percentage => subtotal * voucher.DiscountValue / 100m,
            _ => voucher.DiscountValue
        };

        if (voucher.MaxDiscountValue.HasValue)
        {
            discount = Math.Min(discount, voucher.MaxDiscountValue.Value);
        }

        return Math.Min(Math.Max(0m, discount), subtotal);
    }

    private async Task<User?> ResolveUserAsync(
        string userEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        return await dbContext.Users.FirstOrDefaultAsync(
            item => item.IsActive && item.Email.ToLower() == normalizedEmail,
            cancellationToken);
    }

    private static CheckoutVoucherValidationResult Invalid(string message) =>
        new(false, null, 0m, message);

    private sealed record CartVoucherTargets(
        HashSet<long> VariantIds,
        HashSet<long> ProductIds,
        HashSet<long> CategoryIds,
        HashSet<long> BrandIds);
}
