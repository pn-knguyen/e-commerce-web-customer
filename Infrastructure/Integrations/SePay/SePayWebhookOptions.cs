namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookOptions
{
    public const string SectionName = "SePayWebhook";

    public bool IsTestMode { get; set; } = true;
    public string AuthenticationMode { get; set; } = "None";
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AllowedClockSkewSeconds { get; set; } = 300;
}
