using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.CustomerMessages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace e_commerce_web_customer.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(
    IAiService aiService,
    ICustomerMessageCustomerService customerMessageService,
    ICustomerMessageTokenService tokenService,
    IConfiguration configuration,
    ILogger<ChatController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [EnableRateLimiting("AiChatLimiter")]
    public async Task<ActionResult<AiChatResponse>> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new AiChatResponse(false, null, [], "Vui lòng nhập câu hỏi."));
        }

        var history = (request.History ?? [])
            .TakeLast(8)
            .Where(item => !string.IsNullOrWhiteSpace(item.Message))
            .Select(item => new AiChatMessage(item.Role, item.Message))
            .ToList();

        try
        {
            var question = request.Message.Trim();
            var result = await aiService.AskAsync(
                question,
                history,
                cancellationToken);
            var products = result.Products.Select(product => new AiSuggestedProductDto(
                product.Id,
                product.Name,
                product.Price,
                product.ImageUrl,
                product.CategoryName,
                Url.Action("Details", "Product", new { slug = product.Slug })
                    ?? $"/product/{Uri.EscapeDataString(product.Slug)}"))
                .ToList();
            var persistenceMetadataJson = BuildPersistenceMetadata(products);
            var persistenceReceipt = await TryCreatePersistenceReceiptAsync(
                question,
                result.Reply,
                persistenceMetadataJson,
                cancellationToken);

            return new AiChatResponse(
                true,
                result.Reply,
                products,
                PersistenceReceipt: persistenceReceipt,
                PersistenceMetadataJson: persistenceMetadataJson);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "AI chat request could not be completed.");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new AiChatResponse(false, null, [], exception.Message));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected AI chat error.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new AiChatResponse(false, null, [], "Chatbox đang gặp lỗi. Vui lòng thử lại sau."));
        }
    }

    private async Task<string?> TryCreatePersistenceReceiptAsync(
        string question,
        string reply,
        string metadataJson,
        CancellationToken cancellationToken)
    {
        var customerId = await customerMessageService.ResolveActiveCustomerIdAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            cancellationToken);
        if (!customerId.HasValue)
        {
            return null;
        }

        var aiModel = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        return tokenService.CreateAiReceipt(
            customerId.Value,
            question,
            reply,
            metadataJson,
            "Gemini",
            aiModel);
    }

    private static string BuildPersistenceMetadata(IReadOnlyList<AiSuggestedProductDto> products) =>
        JsonSerializer.Serialize(new
        {
            source = "customer-ai-assistant",
            products,
        }, WebJson);
}

public sealed record ChatRequest(
    [Required, StringLength(1000)] string Message,
    [MaxLength(8)] IReadOnlyList<ChatHistoryItem>? History);

public sealed record ChatHistoryItem(
    [Required, RegularExpression("^(user|assistant)$")] string Role,
    [Required, StringLength(2000)] string Message);

public sealed record AiChatResponse(
    bool Success,
    string? Reply,
    IReadOnlyList<AiSuggestedProductDto> Products,
    string? Message = null,
    string? PersistenceReceipt = null,
    string? PersistenceMetadataJson = null);

public sealed record AiSuggestedProductDto(
    long Id,
    string Name,
    decimal Price,
    string ImageUrl,
    string? CategoryName,
    string DetailUrl);
