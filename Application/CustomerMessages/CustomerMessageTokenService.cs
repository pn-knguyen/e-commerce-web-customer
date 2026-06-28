using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace e_commerce_web_customer.Application.CustomerMessages;

public static class CustomerMessageTokenClaims
{
    public const string CustomerId = "techstore:customer_id";
    public const string Scope = "scope";
    public const string AccessScope = "customer_messages";
    public const string AiReceiptScope = "customer_messages.ai_receipt";
    public const string QuestionHash = "question_hash";
    public const string ReplyHash = "reply_hash";
    public const string MetadataHash = "metadata_hash";
    public const string AiProvider = "ai_provider";
    public const string AiModel = "ai_model";
}

public sealed class CustomerMessageJwtOptions
{
    public const string SectionName = "CustomerMessages:Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string AccessAudience { get; set; } = string.Empty;
    public string AiReceiptAudience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int AiReceiptMinutes { get; set; } = 5;
}

public sealed record CustomerMessageAccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ICustomerMessageTokenService
{
    CustomerMessageAccessToken CreateAccessToken(long customerId);

    string CreateAiReceipt(
        long customerId,
        string question,
        string reply,
        string metadataJson,
        string aiProvider,
        string aiModel);
}

public sealed class CustomerMessageTokenService(
    IOptions<CustomerMessageJwtOptions> options) : ICustomerMessageTokenService
{
    private readonly CustomerMessageJwtOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _handler = new();

    public CustomerMessageAccessToken CreateAccessToken(long customerId)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var token = CreateToken(
            customerId,
            _options.AccessAudience,
            expiresAt,
            [new Claim(CustomerMessageTokenClaims.Scope, CustomerMessageTokenClaims.AccessScope)]);

        return new CustomerMessageAccessToken(token, expiresAt);
    }

    public string CreateAiReceipt(
        long customerId,
        string question,
        string reply,
        string metadataJson,
        string aiProvider,
        string aiModel)
    {
        var claims = new List<Claim>
        {
            new(CustomerMessageTokenClaims.Scope, CustomerMessageTokenClaims.AiReceiptScope),
            new(CustomerMessageTokenClaims.QuestionHash, Hash(question)),
            new(CustomerMessageTokenClaims.ReplyHash, Hash(reply)),
            new(CustomerMessageTokenClaims.MetadataHash, Hash(metadataJson)),
            new(CustomerMessageTokenClaims.AiProvider, aiProvider),
            new(CustomerMessageTokenClaims.AiModel, aiModel),
        };

        return CreateToken(
            customerId,
            _options.AiReceiptAudience,
            DateTimeOffset.UtcNow.AddMinutes(_options.AiReceiptMinutes),
            claims);
    }

    private string CreateToken(
        long customerId,
        string audience,
        DateTimeOffset expiresAt,
        IEnumerable<Claim> additionalClaims)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(CustomerMessageTokenClaims.CustomerId, customerId.ToString()),
            new(JwtRegisteredClaimNames.Sub, customerId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(additionalClaims);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return _handler.WriteToken(token);
    }

    private static string Hash(string value) =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
