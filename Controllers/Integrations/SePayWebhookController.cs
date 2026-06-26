using System.Text.Json;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Infrastructure.Integrations.SePay;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers.Integrations;

[ApiController]
public sealed class SePayWebhookController(
    SePayWebhookAuthenticator authenticator,
    ISePayWebhookService webhookService,
    ILogger<SePayWebhookController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [HttpPost("api/webhooks/sepay")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var authentication = authenticator.Authenticate(Request.Headers, rawBody);
        if (!authentication.IsValid)
        {
            logger.LogWarning("Rejected SePay webhook: {Reason}", authentication.Error);
            return Unauthorized(new { success = false, message = authentication.Error });
        }

        var notification = DeserializeJsonPayload(rawBody);
        if (notification is null)
        {
            return BadRequest(new { success = false, message = "Payload không được để trống." });
        }

        var validationError = notification.Validate();
        if (validationError is not null)
        {
            return BadRequest(new { success = false, message = validationError });
        }

        var result = await webhookService.ReceiveAsync(
            notification,
            rawBody,
            cancellationToken);
        logger.LogInformation(
            "Received SePay webhook {TransactionId}. Duplicate: {IsDuplicate}",
            notification.Id,
            result.IsDuplicate);

        return Ok(new { success = true });
    }

    private static SePayWebhookNotification? DeserializeJsonPayload(string rawBody)
    {
        try
        {
            return JsonSerializer.Deserialize<SePayWebhookNotification>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
