namespace e_commerce_web_customer.Models.Entities;

public sealed class SePayWebhookEvent
{
    public long Id { get; set; }
    public long SePayTransactionId { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? SubAccount { get; set; }
    public string? Code { get; set; }
    public string Content { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal Accumulated { get; set; }
    public string? ReferenceCode { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public bool IsTestMode { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? ProcessingMessage { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public long? MatchedOrderId { get; set; }

    public Order? MatchedOrder { get; set; }
}
