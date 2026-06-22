using System.Text.Json.Serialization;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookPayload
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("gateway")]
    public string Gateway { get; init; } = string.Empty;

    [JsonPropertyName("transactionDate")]
    public string TransactionDate { get; init; } = string.Empty;

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; init; } = string.Empty;

    [JsonPropertyName("subAccount")]
    public string? SubAccount { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("transferType")]
    public string TransferType { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; init; }

    [JsonPropertyName("accumulated")]
    public decimal Accumulated { get; init; }

    [JsonPropertyName("referenceCode")]
    public string? ReferenceCode { get; init; }

    public string? Validate()
    {
        if (Id <= 0) return "id phải lớn hơn 0.";
        if (string.IsNullOrWhiteSpace(Gateway)) return "gateway là bắt buộc.";
        if (string.IsNullOrWhiteSpace(TransactionDate)) return "transactionDate là bắt buộc.";
        if (string.IsNullOrWhiteSpace(AccountNumber)) return "accountNumber là bắt buộc.";
        if (string.IsNullOrWhiteSpace(Content)) return "content là bắt buộc.";
        if (TransferType is not ("in" or "out")) return "transferType phải là in hoặc out.";
        if (TransferAmount <= 0) return "transferAmount phải lớn hơn 0.";
        return null;
    }
}
