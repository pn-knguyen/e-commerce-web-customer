using System;
using System.Collections.Generic;

namespace e_commerce_web_customer.Models;

public partial class GoodsReceipt
{
    public long Id { get; set; }

    public long SupplierId { get; set; }

    public string ReceiptCode { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public long CreatedBy { get; set; }

    public long? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<GoodReceiptItem> GoodReceiptItems { get; set; } = new List<GoodReceiptItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
