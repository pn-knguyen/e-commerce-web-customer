using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Orders;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.Services;

public sealed class DbOrderService(EcommerceDbContext dbContext)
    : IOrderService
{
    public async Task<PlacedOrder> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestedItems = request.Items
            .Where(item => item.Quantity > 0)
            .Select(item => new
            {
                Line = item,
                HasVariantId = long.TryParse(item.ProductId, out var variantId),
                VariantId = variantId
            })
            .ToList();

        if (requestedItems.Count == 0 ||
            requestedItems.Any(item => !item.HasVariantId || item.VariantId <= 0))
        {
            throw new OrderPlacementException("Order contains an invalid product.");
        }

        var quantities = requestedItems
            .GroupBy(item => item.VariantId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Line.Quantity));
        var user = await dbContext.Users.FirstOrDefaultAsync(
            item => item.Email == request.Email.Trim().ToLowerInvariant() && item.IsActive,
            cancellationToken);

        if (user is null)
        {
            throw new OrderPlacementException("The customer account is unavailable.");
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var variants = await dbContext.ProductVariants
            .Include(item => item.Product)
            .Where(item => quantities.Keys.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (variants.Count != quantities.Count ||
            variants.Any(item => !item.IsActive || !item.Product.IsActive))
        {
            throw new OrderPlacementException("One or more products are unavailable.");
        }

        foreach (var variant in variants)
        {
            var quantity = quantities[variant.Id];
            if (variant.Quantity > 0 && quantity > variant.Quantity)
            {
                throw new OrderPlacementException(
                    $"Product {variant.Product.Name} has insufficient stock.");
            }
        }

        var paymentMethod = await ResolvePaymentMethodAsync(
            request.PaymentMethod,
            cancellationToken);
        var placedAt = DateTimeOffset.Now;
        var subtotal = variants.Sum(item => item.Price * quantities[item.Id]);
        var discount = Math.Clamp(request.Discount, 0, subtotal);
        var shippingFee = Math.Max(0, request.ShippingFee);
        var order = new Order
        {
            UserId = user.Id,
            PaymentMethodId = paymentMethod.Id,
            OrderCode = BuildOrderCode(),
            ShippingContactName = request.CustomerName.Trim(),
            ShippingPhone = request.Phone.Trim(),
            ShippingProvince = string.Empty,
            ShippingWard = string.Empty,
            ShippingDetail = request.DeliveryAddress.Trim(),
            SubtotalAmount = subtotal,
            ShippingFee = shippingFee,
            VoucherDiscount = discount,
            TotalAmount = subtotal + shippingFee - discount,
            OrderStatus = "Pending",
            PaymentStatus = "Unpaid",
            CreatedAt = placedAt.UtcDateTime,
            OrderItems = variants.Select(item => new OrderItem
            {
                ProductVariantId = item.Id,
                Quantity = quantities[item.Id],
                UnitPrice = item.Price
            }).ToList()
        };

        foreach (var variant in variants.Where(item => item.Quantity > 0))
        {
            var quantity = quantities[variant.Id];
            variant.Quantity -= quantity;
            variant.SoldCount += quantity;
            variant.Product.TotalSoldCount += quantity;
            variant.UpdatedAt = placedAt.UtcDateTime;
            variant.Product.UpdatedAt = placedAt.UtcDateTime;
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PlacedOrder(
            order.OrderCode,
            placedAt,
            placedAt.AddDays(2));
    }

    private async Task<Models.PaymentMethod> ResolvePaymentMethodAsync(
        string requestedMethod,
        CancellationToken cancellationToken)
    {
        var methods = await dbContext.PaymentMethods
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var aliases = requestedMethod switch
        {
            "BankTransfer" => new[] { "bank", "transfer" },
            "Momo" => new[] { "momo" },
            "VnPay" => new[] { "vnpay" },
            "ZaloPay" => new[] { "zalopay", "zalo pay" },
            _ => new[] { "cod", "cash" }
        };
        var method = methods.FirstOrDefault(item =>
            aliases.Any(alias =>
                item.Name.Contains(alias, StringComparison.OrdinalIgnoreCase)));

        if (method is null && requestedMethod == "Cod")
        {
            method = methods.FirstOrDefault();
        }

        return method ?? throw new OrderPlacementException(
            "The selected payment method is unavailable.");
    }

    private static string BuildOrderCode()
    {
        return $"TS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 1000)}";
    }
}
