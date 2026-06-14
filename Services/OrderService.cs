using e_commerce_web_customer.Data;
using e_commerce_web_customer.DTOs.Order;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Models;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Services;

public class OrderService : IOrderService
{
    private readonly EcommerceDbContext _dbContext;

    public OrderService(EcommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Đơn hàng không có sản phẩm.");
        }

        var requestedItems = request.Items
            .Where(item => item.ProductVariantId > 0 && item.Quantity > 0)
            .GroupBy(item => item.ProductVariantId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        if (requestedItems.Count == 0)
        {
            throw new InvalidOperationException("Đơn hàng không có sản phẩm hợp lệ.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var variants = await _dbContext.ProductVariants
            .Include(variant => variant.Product)
            .Include(variant => variant.ProductVariantImages)
            .Where(variant => requestedItems.Keys.Contains(variant.Id))
            .ToListAsync();

        if (variants.Count != requestedItems.Count ||
            variants.Any(variant => !variant.IsActive || !variant.Product.IsActive))
        {
            throw new InvalidOperationException("Một hoặc nhiều sản phẩm không còn khả dụng.");
        }

        foreach (var variant in variants)
        {
            var quantity = requestedItems[variant.Id];

            if (variant.Quantity > 0 && quantity > variant.Quantity)
            {
                throw new InvalidOperationException(
                    $"Sản phẩm {variant.Product.Name} chỉ còn {variant.Quantity} sản phẩm.");
            }
        }

        var paymentMethod = await ResolvePaymentMethodAsync(request.PaymentMethod);
        var subtotal = variants.Sum(variant => variant.Price * requestedItems[variant.Id]);
        var shippingFee = Math.Max(0, request.ShippingFee);
        var createdAt = DateTime.UtcNow;
        var order = new Order
        {
            UserId = request.UserId,
            PaymentMethodId = paymentMethod.Id,
            OrderCode = BuildOrderCode(),
            ShippingContactName = request.ContactName.Trim(),
            ShippingPhone = request.Phone.Trim(),
            ShippingProvince = request.Province.Trim(),
            ShippingWard = request.Ward.Trim(),
            ShippingDetail = request.DetailAddress.Trim(),
            SubtotalAmount = subtotal,
            ShippingFee = shippingFee,
            VoucherDiscount = 0,
            TotalAmount = subtotal + shippingFee,
            OrderStatus = "Pending",
            PaymentStatus = "Unpaid",
            CreatedAt = createdAt,
            OrderItems = variants.Select(variant => new OrderItem
            {
                ProductVariantId = variant.Id,
                Quantity = requestedItems[variant.Id],
                UnitPrice = variant.Price
            }).ToList()
        };

        foreach (var variant in variants.Where(variant => variant.Quantity > 0))
        {
            variant.Quantity -= requestedItems[variant.Id];
            variant.SoldCount += requestedItems[variant.Id];
            variant.Product.TotalSoldCount += requestedItems[variant.Id];
            variant.UpdatedAt = createdAt;
            variant.Product.UpdatedAt = createdAt;
        }

        _dbContext.Orders.Add(order);

        var orderedVariantIds = requestedItems.Keys.ToList();
        var cartItems = await _dbContext.CartItems
            .Where(item =>
                item.UserId == request.UserId &&
                orderedVariantIds.Contains(item.ProductVariantId))
            .ToListAsync();
        _dbContext.CartItems.RemoveRange(cartItems);

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapToDto(order, paymentMethod.Name, variants);
    }

    public Task<OrderDto?> GetByIdAsync(long userId, long orderId)
    {
        return OrdersQuery(userId)
            .Where(order => order.Id == orderId)
            .FirstOrDefaultAsync();
    }

    public Task<List<OrderDto>> GetByUserAsync(long userId)
    {
        return OrdersQuery(userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();
    }

    private IQueryable<OrderDto> OrdersQuery(long userId)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .Select(order => new OrderDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                PaymentMethodName = order.PaymentMethod.Name,
                SubtotalAmount = order.SubtotalAmount,
                ShippingFee = order.ShippingFee,
                VoucherDiscount = order.VoucherDiscount,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems.Select(item => new OrderItemDto
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductVariant.Product.Name,
                    VariantName = item.ProductVariant.ColorName ?? item.ProductVariant.Code,
                    ImagePath = item.ProductVariant.ProductVariantImages
                        .OrderBy(image => image.Position)
                        .Select(image => image.ImagePath)
                        .FirstOrDefault(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            });
    }

    private async Task<Models.PaymentMethod> ResolvePaymentMethodAsync(string requestedMethod)
    {
        var methods = await _dbContext.PaymentMethods
            .Where(method => method.IsActive)
            .ToListAsync();
        var aliases = requestedMethod switch
        {
            "BankTransfer" => new[] { "bank", "chuyển khoản", "transfer" },
            "Momo" => new[] { "momo" },
            "VnPay" => new[] { "vnpay" },
            "ZaloPay" => new[] { "zalopay", "zalo pay" },
            _ => new[] { "cod", "nhận hàng", "cash" }
        };
        var method = methods.FirstOrDefault(item =>
            aliases.Any(alias => item.Name.Contains(alias, StringComparison.OrdinalIgnoreCase)));

        if (method is not null)
        {
            return method;
        }

        if (requestedMethod == "Cod" && methods.Count > 0)
        {
            return methods[0];
        }

        throw new InvalidOperationException(
            "Phương thức thanh toán đã chọn chưa được cấu hình hoặc đang bị tắt.");
    }

    private static string BuildOrderCode()
    {
        return $"TS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 1000)}";
    }

    private static OrderDto MapToDto(
        Order order,
        string paymentMethodName,
        IEnumerable<ProductVariant> variants)
    {
        var variantsById = variants.ToDictionary(variant => variant.Id);

        return new OrderDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            PaymentMethodName = paymentMethodName,
            SubtotalAmount = order.SubtotalAmount,
            ShippingFee = order.ShippingFee,
            VoucherDiscount = order.VoucherDiscount,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(item =>
            {
                var variant = variantsById[item.ProductVariantId];

                return new OrderItemDto
                {
                    ProductVariantId = variant.Id,
                    ProductName = variant.Product.Name,
                    VariantName = variant.ColorName ?? variant.Code,
                    ImagePath = variant.ProductVariantImages
                        .OrderBy(image => image.Position)
                        .FirstOrDefault()
                        ?.ImagePath,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
            }).ToList()
        };
    }
}
