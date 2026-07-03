using System.Data;
using System.Globalization;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Orders;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Orders.Db;

public sealed class DbOrderService(EcommerceDbContext dbContext) : IOrderService
{
    public async Task<PlacedOrder> PlaceOrderAsync(
        PlaceOrderRequest request,
        bool clearCart = true,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var user = await ResolveUserAsync(request.UserEmail, cancellationToken);
            var shippingAddress = await ResolveShippingAddressAsync(
                user.Id,
                request.ShippingAddressId,
                cancellationToken);
            var paymentMethod = await ResolvePaymentMethodAsync(
                request.PaymentMethodId,
                cancellationToken);
            var orderLines = await ResolveOrderLinesAsync(
                request.Items,
                cancellationToken);

            var subtotal = orderLines.Sum(line => line.Variant.Price * line.Quantity);
            var shippingFee = Math.Max(0m, request.ShippingFee);
            var resolvedVoucher = await ResolveVoucherAsync(
                request.VoucherId,
                user.Id,
                subtotal,
                orderLines,
                cancellationToken);
            var discount = resolvedVoucher.DiscountAmount;
            var now = DateTime.UtcNow;
            var orderCode = await GenerateOrderCodeAsync(now, cancellationToken);

            var order = new Order
            {
                UserId = user.Id,
                PaymentMethodId = paymentMethod.Id,
                VoucherId = resolvedVoucher.Voucher?.Id,
                OrderCode = orderCode,
                ShippingAddressId = shippingAddress?.Id,
                ShippingContactName = shippingAddress?.ContactName.Trim() ?? request.CustomerName.Trim(),
                ShippingPhone = shippingAddress?.Phone.Trim() ?? request.Phone.Trim(),
                ShippingProvince = shippingAddress?.ProvinceName.Trim() ?? request.ShippingProvince.Trim(),
                ShippingWard = shippingAddress?.WardName.Trim() ?? request.ShippingWard.Trim(),
                ShippingDetail = shippingAddress is null
                    ? request.ShippingDetail.Trim()
                    : BuildShippingDetail(shippingAddress),
                SubtotalAmount = subtotal,
                ShippingFee = shippingFee,
                VoucherDiscount = discount,
                TotalAmount = subtotal + shippingFee - discount,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                CreatedAt = now
            };

            foreach (var line in orderLines)
            {
                line.Variant.Quantity -= line.Quantity;
                line.Variant.SoldCount += line.Quantity;

                if (line.Variant.Product is not null)
                {
                    line.Variant.Product.TotalSoldCount += line.Quantity;
                }

                order.OrderItems.Add(new OrderItem
                {
                    ProductVariantId = line.Variant.Id,
                    Quantity = line.Quantity,
                    UnitPrice = line.Variant.Price
                });
            }

            dbContext.Orders.Add(order);

            if (resolvedVoucher.Voucher is not null && discount > 0)
            {
                ApplyVoucherUsage(
                    resolvedVoucher.Voucher,
                    user.Id,
                    order);
            }

            var orderedVariantIds = orderLines
                .Select(line => line.Variant.Id)
                .ToHashSet();
            var completedCartItems = await dbContext.CartItems
                .Where(item =>
                    item.UserId == user.Id
                    && orderedVariantIds.Contains(item.ProductVariantId))
                .ToListAsync(cancellationToken);

            if (clearCart && completedCartItems.Count > 0)
            {
                dbContext.CartItems.RemoveRange(completedCartItems);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var placedAt = new DateTimeOffset(now, TimeSpan.Zero).ToLocalTime();
            return new PlacedOrder(
                orderCode,
                placedAt,
                placedAt.AddDays(2));
        }
        catch (OrderPlacementException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new OrderPlacementException(
                "Không thể lưu đơn hàng. Vui lòng kiểm tra lại sản phẩm và thử lại.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new OrderPlacementException(
                "Đặt hàng chưa thành công. Vui lòng thử lại sau.");
        }
    }

    public async Task UpdatePaymentStatusAsync(
        string orderCode,
        bool isPaid,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

        if (order is null) return;

        order.PaymentStatus = isPaid ? PaymentStatus.Paid : PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelFailedOrderAsync(
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.ProductVariant)
            .ThenInclude(pv => pv!.Product)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

        if (order is null || order.OrderStatus == OrderStatus.Cancelled) return;

        order.OrderStatus = OrderStatus.Cancelled;
        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;

        // Restore stock
        foreach (var line in order.OrderItems)
        {
            if (line.ProductVariant is not null)
            {
                line.ProductVariant.Quantity += line.Quantity;
                line.ProductVariant.SoldCount = Math.Max(0, line.ProductVariant.SoldCount - line.Quantity);

                if (line.ProductVariant.Product is not null)
                {
                    line.ProductVariant.Product.TotalSoldCount = Math.Max(0, line.ProductVariant.Product.TotalSoldCount - line.Quantity);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmOnlinePaymentAsync(
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

        if (order is null) return;

        order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        // Clear the cart items corresponding to this order
        var orderedVariantIds = order.OrderItems.Select(oi => oi.ProductVariantId).ToHashSet();
        var completedCartItems = await dbContext.CartItems
            .Where(item => item.UserId == order.UserId && orderedVariantIds.Contains(item.ProductVariantId))
            .ToListAsync(cancellationToken);

        if (completedCartItems.Count > 0)
        {
            dbContext.CartItems.RemoveRange(completedCartItems);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderForPayment?> GetOrderForPaymentAsync(
        string orderCode,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        var order = await dbContext.Orders
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.OrderCode == orderCode 
                  && o.User != null 
                  && o.User.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (order is null) return null;

        return new OrderForPayment(order.OrderCode, order.TotalAmount);
    }



    private static void ValidateRequest(PlaceOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserEmail))
        {
            throw new OrderPlacementException("Không xác định được tài khoản đặt hàng.");
        }

        if (request.Items.Count == 0)
        {
            throw new OrderPlacementException("Đơn hàng không có sản phẩm.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName)
            || string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.ShippingProvince)
            || string.IsNullOrWhiteSpace(request.ShippingWard)
            || string.IsNullOrWhiteSpace(request.ShippingDetail))
        {
            throw new OrderPlacementException("Thông tin giao hàng chưa đầy đủ.");
        }
    }

    private async Task<User> ResolveUserAsync(
        string userEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userEmail.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(
            item => item.IsActive && item.Email.ToLower() == normalizedEmail,
            cancellationToken);

        return user
            ?? throw new OrderPlacementException("Tài khoản đặt hàng không còn hoạt động.");
    }

    private async Task<UserAddress?> ResolveShippingAddressAsync(
        long userId,
        long? shippingAddressId,
        CancellationToken cancellationToken)
    {
        if (shippingAddressId is null)
        {
            return null;
        }

        var address = await dbContext.UserAddresses
            .FirstOrDefaultAsync(
                item =>
                    item.Id == shippingAddressId.Value
                    && item.UserId == userId
                    && !item.IsDeleted,
                cancellationToken);

        return address
            ?? throw new OrderPlacementException("Địa chỉ giao hàng không còn khả dụng. Vui lòng chọn lại địa chỉ.");
    }

    private async Task<PaymentMethod> ResolvePaymentMethodAsync(
        long paymentMethodId,
        CancellationToken cancellationToken)
    {
        var method = await dbContext.PaymentMethods
            .FirstOrDefaultAsync(
                item => item.Id == paymentMethodId && item.IsActive,
                cancellationToken);

        return method
            ?? throw new OrderPlacementException(
                "Phương thức thanh toán hiện không khả dụng.");
    }

    private static string BuildShippingDetail(UserAddress address)
    {
        var parts = new[]
        {
            address.DetailAddress,
            address.DistrictName
        }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        return string.Join(", ", parts);
    }

    private async Task<IReadOnlyList<ResolvedOrderLine>> ResolveOrderLinesAsync(
        IReadOnlyList<PlaceOrderLine> requestedLines,
        CancellationToken cancellationToken)
    {
        var normalizedLines = requestedLines
            .Where(line => !string.IsNullOrWhiteSpace(line.ProductId))
            .GroupBy(line => line.ProductId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Key = group.Key,
                Quantity = group.Sum(line => Math.Max(1, line.Quantity))
            })
            .ToList();

        if (normalizedLines.Count == 0)
        {
            throw new OrderPlacementException("Đơn hàng không có sản phẩm hợp lệ.");
        }

        var resolvedLines = new List<ResolvedOrderLine>();
        foreach (var line in normalizedLines)
        {
            ProductVariant? variant;
            if (long.TryParse(
                line.Key,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var variantId))
            {
                variant = await dbContext.ProductVariants
                    .Include(item => item.Product)
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == variantId
                            && item.IsActive
                            && item.Product != null
                            && item.Product.IsActive,
                        cancellationToken);
            }
            else
            {
                variant = await dbContext.ProductVariants
                    .Include(item => item.Product)
                    .FirstOrDefaultAsync(
                        item =>
                            item.Code == line.Key
                            && item.IsActive
                            && item.Product != null
                            && item.Product.IsActive,
                        cancellationToken);
            }

            if (variant is null)
            {
                throw new OrderPlacementException(
                    $"Sản phẩm có mã {line.Key} không còn khả dụng.");
            }

            if (variant.Quantity < line.Quantity)
            {
                throw new OrderPlacementException(
                    $"Sản phẩm {variant.Product!.Name} chỉ còn {variant.Quantity} sản phẩm.");
            }

            resolvedLines.Add(new ResolvedOrderLine(variant, line.Quantity));
        }

        return resolvedLines;
    }

    private async Task<string> GenerateOrderCodeAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var orderCode = $"ORD-{now:yyyyMMdd}-{suffix}";
            var exists = await dbContext.Orders
                .AsNoTracking()
                .AnyAsync(order => order.OrderCode == orderCode, cancellationToken);

            if (!exists)
            {
                return orderCode;
            }
        }

        throw new OrderPlacementException(
            "Không thể tạo mã đơn hàng. Vui lòng thử lại.");
    }

    private async Task<ResolvedVoucher> ResolveVoucherAsync(
        long? voucherId,
        long userId,
        decimal subtotal,
        IReadOnlyCollection<ResolvedOrderLine> orderLines,
        CancellationToken cancellationToken)
    {
        if (voucherId is null)
        {
            return new ResolvedVoucher(null, 0m);
        }

        var voucher = await dbContext.Vouchers
            .Include(item => item.VoucherUsers)
            .Include(item => item.VoucherUsages)
            .Include(item => item.VoucherTargets)
            .FirstOrDefaultAsync(item => item.Id == voucherId.Value, cancellationToken);

        if (voucher is null)
        {
            throw new OrderPlacementException("Voucher đã chọn không còn tồn tại.");
        }

        var now = DateTime.UtcNow;
        if (!voucher.IsActive || voucher.StartDate > now || voucher.EndDate < now)
        {
            throw new OrderPlacementException("Voucher đã hết hạn hoặc không còn hoạt động.");
        }

        if (subtotal < voucher.MinOrderValue)
        {
            throw new OrderPlacementException("Đơn hàng chưa đạt giá trị tối thiểu để dùng voucher.");
        }

        if (voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value)
        {
            throw new OrderPlacementException("Voucher đã hết lượt sử dụng.");
        }

        var userUsedCount = voucher.VoucherUsages.Count(item => item.UserId == userId);
        if (voucher.MaxUsesPerUser.HasValue && userUsedCount >= voucher.MaxUsesPerUser.Value)
        {
            throw new OrderPlacementException("Bạn đã dùng hết lượt cho voucher này.");
        }

        var voucherUser = voucher.VoucherUsers.FirstOrDefault(item => item.UserId == userId);
        if (voucher.VoucherUsers.Count > 0 && voucherUser is null)
        {
            throw new OrderPlacementException("Voucher này không dành cho tài khoản của bạn.");
        }

        if (voucherUser is not null && voucherUser.UsedCount >= voucherUser.MaxUses)
        {
            throw new OrderPlacementException("Bạn đã dùng hết lượt được cấp cho voucher này.");
        }

        if (voucher.VoucherTargets.Count > 0 && !TargetsOrderLines(voucher.VoucherTargets, orderLines))
        {
            throw new OrderPlacementException("Voucher không áp dụng cho sản phẩm trong giỏ hàng.");
        }

        return new ResolvedVoucher(voucher, CalculateVoucherDiscount(voucher, subtotal));
    }

    private static bool TargetsOrderLines(
        IEnumerable<VoucherTarget> voucherTargets,
        IReadOnlyCollection<ResolvedOrderLine> orderLines)
    {
        var variantIds = orderLines.Select(line => line.Variant.Id).ToHashSet();
        var productIds = orderLines.Select(line => line.Variant.ProductId).ToHashSet();
        var categoryIds = orderLines
            .Where(line => line.Variant.Product is not null)
            .Select(line => line.Variant.Product!.CategoryId)
            .ToHashSet();
        var brandIds = orderLines
            .Where(line => line.Variant.Product is not null)
            .Select(line => line.Variant.Product!.BrandId)
            .ToHashSet();

        return voucherTargets.Any(target => target.TargetType switch
        {
            TargetType.Product => productIds.Contains(target.TargetId),
            TargetType.ProductVariant => variantIds.Contains(target.TargetId),
            TargetType.Category => categoryIds.Contains(target.TargetId),
            TargetType.Brand => brandIds.Contains(target.TargetId),
            _ => false
        });
    }

    private static decimal CalculateVoucherDiscount(Voucher voucher, decimal subtotal)
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

    private static void ApplyVoucherUsage(
        Voucher voucher,
        long userId,
        Order order)
    {
        voucher.UsedCount += 1;
        voucher.UpdatedAt = DateTime.UtcNow;

        var voucherUser = voucher.VoucherUsers.FirstOrDefault(item => item.UserId == userId);
        if (voucherUser is not null)
        {
            voucherUser.UsedCount += 1;
        }

        order.VoucherUsages.Add(new VoucherUsage
        {
            VoucherId = voucher.Id,
            UserId = userId,
            UsedAt = DateTime.UtcNow
        });
    }

    private sealed record ResolvedVoucher(
        Voucher? Voucher,
        decimal DiscountAmount);

    private sealed record ResolvedOrderLine(
        ProductVariant Variant,
        int Quantity);
}
