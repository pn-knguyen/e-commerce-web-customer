using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class Order
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long PaymentMethodId { get; set; }

    public long? VoucherId { get; set; }

    public string OrderCode { get; set; } = null!;

    public long? ShippingAddressId { get; set; }

    public string ShippingContactName { get; set; } = null!;

    public string ShippingPhone { get; set; } = null!;

    public string ShippingProvince { get; set; } = null!;

    public string ShippingWard { get; set; } = null!;

    public string ShippingDetail { get; set; } = null!;

    public decimal SubtotalAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal VoucherDiscount { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual UserAddress? ShippingAddress { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }

    public virtual VoucherUsage? VoucherUsage { get; set; }
}
