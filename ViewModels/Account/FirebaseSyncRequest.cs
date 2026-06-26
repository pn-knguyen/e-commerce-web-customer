namespace e_commerce_web_customer.ViewModels.Account;

public sealed class FirebaseSyncRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SessionId { get; set; }
}
