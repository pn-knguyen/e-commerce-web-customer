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
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var authentication = authenticator.Authenticate(Request.Headers, rawBody);
        if (!authentication.IsValid)
        {
            logger.LogWarning("Rejected SePay webhook: {Reason}", authentication.Error);
            return Unauthorized(new { success = false, message = authentication.Error });
        }

        SePayWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SePayWebhookPayload>(rawBody, JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid SePay webhook JSON payload.");
            return BadRequest(new { success = false, message = "Payload JSON không hợp lệ." });
        }

        if (payload is null)
            return BadRequest(new { success = false, message = "Payload không được để trống." });

        var validationError = payload.Validate();
        if (validationError is not null)
            return BadRequest(new { success = false, message = validationError });

        var result = await webhookService.ReceiveAsync(payload, rawBody, cancellationToken);
        logger.LogInformation(
            "Received SePay webhook {TransactionId}. Duplicate: {IsDuplicate}",
            payload.Id,
            result.IsDuplicate);

        return Ok(new { success = true });
    }
}
