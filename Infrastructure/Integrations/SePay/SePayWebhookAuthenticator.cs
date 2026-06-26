using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

public sealed class SePayWebhookAuthenticator(IOptions<SePayWebhookOptions> options)
{
    private readonly SePayWebhookOptions _options = options.Value;

    public SePayAuthenticationResult Authenticate(
        IHeaderDictionary headers,
        string rawBody)
    {
        return _options.AuthenticationMode switch
        {
            SePayAuthenticationMode.None =>
                new(false, "Không được tắt xác thực webhook."),
            SePayAuthenticationMode.ApiKey => AuthenticateApiKey(headers),
            SePayAuthenticationMode.HmacSha256 => AuthenticateHmac(headers, rawBody),
            _ => new(false, "SePayWebhook:AuthenticationMode không hợp lệ.")
        };
    }

    private SePayAuthenticationResult AuthenticateApiKey(IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return new(false, "SePay API key chưa được cấu hình.");

        var authorization = headers.Authorization.ToString();
        const string prefix = "Apikey ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return new(false, "Thiếu Authorization: Apikey.");

        return FixedTimeEquals(authorization[prefix.Length..], _options.ApiKey)
            ? SePayAuthenticationResult.Success
            : new(false, "SePay API key không hợp lệ.");
    }

    private SePayAuthenticationResult AuthenticateHmac(
        IHeaderDictionary headers,
        string rawBody)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return new(false, "SePay webhook secret chưa được cấu hình.");

        var signature = headers["X-SePay-Signature"].ToString();
        var timestampText = headers["X-SePay-Timestamp"].ToString();
        if (!long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            return new(false, "X-SePay-Timestamp không hợp lệ.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var allowedClockSkew = Math.Clamp(_options.AllowedClockSkewSeconds, 30, 3600);
        if (Math.Abs(now - timestamp) > allowedClockSkew)
            return new(false, "Webhook đã hết hạn.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        var signedData = Encoding.UTF8.GetBytes($"{timestampText}.{rawBody}");
        var expected = $"sha256={Convert.ToHexString(hmac.ComputeHash(signedData)).ToLowerInvariant()}";

        return FixedTimeEquals(signature.Trim().ToLowerInvariant(), expected)
            ? SePayAuthenticationResult.Success
            : new(false, "Chữ ký SePay không hợp lệ.");
    }

    private static bool FixedTimeEquals(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}

public sealed record SePayAuthenticationResult(bool IsValid, string? Error)
{
    public static readonly SePayAuthenticationResult Success = new(true, null);
}
