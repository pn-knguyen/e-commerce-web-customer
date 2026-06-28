using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.CustomerMessages;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

[ApiController]
[Route("api/customer-messages")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerMessagesController(
    ICustomerMessageCustomerService customerMessageService,
    ICustomerMessageTokenService tokenService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("bootstrap")]
    public async Task<ActionResult<CustomerChatBootstrapResponse>> Bootstrap(
        [FromQuery] string? channel,
        CancellationToken cancellationToken)
    {
        var bootstrap = await customerMessageService.GetBootstrapAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            HttpContext.Session.GetString(SessionKeys.UserDisplayName),
            HttpContext.Session.GetString(SessionKeys.UserPhoneNumber),
            channel,
            cancellationToken);

        var accessToken = bootstrap.UserId.HasValue
            ? tokenService.CreateAccessToken(bootstrap.UserId.Value)
            : null;

        return new CustomerChatBootstrapResponse(
            bootstrap.IsLoggedIn,
            bootstrap.UserId,
            bootstrap.DisplayName,
            bootstrap.ConversationId,
            bootstrap.Subject,
            bootstrap.Messages,
            ResolveHubUrl(),
            "Gemini",
            configuration["Gemini:Model"] ?? "gemini-2.5-flash",
            accessToken?.Value,
            accessToken?.ExpiresAt);
    }

    [HttpGet("access-token")]
    public async Task<ActionResult<CustomerMessageAccessTokenResponse>> AccessToken(
        CancellationToken cancellationToken)
    {
        var customerId = await customerMessageService.ResolveActiveCustomerIdAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            cancellationToken);
        if (!customerId.HasValue)
        {
            return Unauthorized(new { message = "Phiên đăng nhập khách hàng không hợp lệ." });
        }

        var accessToken = tokenService.CreateAccessToken(customerId.Value);
        return new CustomerMessageAccessTokenResponse(accessToken.Value, accessToken.ExpiresAt);
    }

    private string ResolveHubUrl()
    {
        var configuredUrl = configuration["CustomerMessages:HubUrl"];
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl.Trim();
        }

        if (environment.IsDevelopment())
        {
            return "http://localhost:5081/hubs/customer-messages";
        }

        throw new InvalidOperationException(
            "CustomerMessages:HubUrl must be configured before customer chat can start.");
    }
}

public sealed record CustomerChatBootstrapResponse(
    bool IsLoggedIn,
    long? UserId,
    string DisplayName,
    long? ConversationId,
    string? Subject,
    IReadOnlyList<CustomerChatMessage> Messages,
    string HubUrl,
    string AiProvider,
    string AiModel,
    string? RealtimeAccessToken,
    DateTimeOffset? RealtimeAccessTokenExpiresAt);

public sealed record CustomerMessageAccessTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt);
