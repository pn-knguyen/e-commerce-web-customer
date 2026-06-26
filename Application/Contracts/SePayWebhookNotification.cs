namespace e_commerce_web_customer.Application.Contracts;

public sealed class SePayWebhookNotification
{
    public long Id { get; init; }
    public string Gateway { get; init; } = string.Empty;
    public string TransactionDate { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? SubAccount { get; init; }
    public string? Code { get; init; }
    public string Content { get; init; } = string.Empty;
    public string TransferType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal TransferAmount { get; init; }
    public decimal Accumulated { get; init; }
    public string? ReferenceCode { get; init; }

    public string? Validate()
    {
        // SePay sandbox notifications may use id = 0.
        if (string.IsNullOrWhiteSpace(Gateway)) return "gateway is required.";
        if (string.IsNullOrWhiteSpace(TransactionDate)) return "transactionDate is required.";
        if (string.IsNullOrWhiteSpace(AccountNumber)) return "accountNumber is required.";
        if (string.IsNullOrWhiteSpace(Content)) return "content is required.";
        if (!string.Equals(TransferType, "in", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(TransferType, "out", StringComparison.OrdinalIgnoreCase))
        {
            return "transferType must be in or out.";
        }

        if (TransferAmount <= 0) return "transferAmount must be greater than 0.";
        return null;
    }
}
