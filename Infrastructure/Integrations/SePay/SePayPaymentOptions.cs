namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayPaymentOptions
{
    public const string SectionName = "SePayPayment";

    public long PaymentMethodId { get; set; } = 7;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int PaymentTimeoutMinutes { get; set; } = 15;
    public bool IsTestMode { get; set; } = true;
}
