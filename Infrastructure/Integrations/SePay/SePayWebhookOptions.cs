namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookOptions
{
    public const string SectionName = "SePayWebhook";

    public bool IsTestMode { get; set; } = true;
    public SePayAuthenticationMode AuthenticationMode { get; set; } =
        SePayAuthenticationMode.HmacSha256;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AllowedClockSkewSeconds { get; set; } = 300;
}

public enum SePayAuthenticationMode
{
    HmacSha256,
    ApiKey,
    None
}
