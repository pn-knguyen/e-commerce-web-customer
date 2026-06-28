using System.Globalization;
using e_commerce_web_customer.Application.CustomerMessages;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Models.Entities;
using e_commerce_web_customer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Infrastructure.CustomerMessages;

public sealed class DbCustomerMessageCustomerService(
    EcommerceDbContext dbContext) : ICustomerMessageCustomerService
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public async Task<long?> ResolveActiveCustomerIdAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(item =>
                item.Email == normalizedEmail &&
                item.Role == UserRole.Customer &&
                item.IsActive)
            .Select(item => (long?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerChatBootstrap> GetBootstrapAsync(
        string? email,
        string? displayName,
        string? phoneNumber,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        var conversationChannel = NormalizeChannel(channel);
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return GuestBootstrap(displayName);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item =>
                item.Email == normalizedEmail &&
                item.Role == UserRole.Customer &&
                item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.FullName,
                item.Email,
                item.Phone,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return GuestBootstrap(displayName);
        }

        var conversation = await dbContext.CustomerConversations
            .AsNoTracking()
            .Where(item =>
                item.UserId == user.Id &&
                item.Channel == conversationChannel)
            .OrderByDescending(item => item.Status != CustomerConversationStatus.Closed)
            .ThenByDescending(item => item.LastMessageAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Subject,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return new CustomerChatBootstrap(
                true,
                user.Id,
                ResolveDisplayName(user.FullName, displayName, user.Email),
                null,
                null,
                []);
        }

        var messages = await dbContext.CustomerMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == conversation.Id)
            .OrderByDescending(item => item.Id)
            .Take(100)
            .Select(item => new
            {
                item.Id,
                item.ConversationId,
                item.Sender,
                item.Body,
                item.AiProvider,
                item.AiModel,
                item.AiMetadataJson,
                item.CreatedAt,
                CustomerName = item.User != null ? item.User.FullName : user.FullName,
                StaffName = item.Staff != null ? item.Staff.FullName : null,
            })
            .ToListAsync(cancellationToken);

        return new CustomerChatBootstrap(
            true,
            user.Id,
            ResolveDisplayName(user.FullName, displayName, user.Email),
            conversation.Id,
            conversation.Subject,
            messages
                .OrderBy(item => item.Id)
                .Select(item => new CustomerChatMessage(
                    item.Id,
                    item.ConversationId,
                    item.Sender.ToString(),
                    GetSenderLabel(item.Sender),
                    GetSenderName(item.Sender, item.CustomerName, item.StaffName, item.AiModel),
                    item.Body,
                    item.AiProvider,
                    item.AiModel,
                    CustomerMessageMetadataSanitizer.Sanitize(item.AiMetadataJson),
                    item.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                    FormatDate(item.CreatedAt)))
                .ToList());
    }

    private static CustomerChatBootstrap GuestBootstrap(string? displayName) =>
        new(false, null, displayName?.Trim() ?? string.Empty, null, null, []);

    private static CustomerConversationChannel NormalizeChannel(string? channel) =>
        string.Equals(channel, "ai", StringComparison.OrdinalIgnoreCase)
            ? CustomerConversationChannel.Ai
            : CustomerConversationChannel.Support;

    private static string ResolveDisplayName(
        string? fullName,
        string? sessionName,
        string email)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            return sessionName.Trim();
        }

        return email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? email;
    }

    private static string GetSenderLabel(CustomerMessageSender sender) => sender switch
    {
        CustomerMessageSender.Customer => "Khách hàng",
        CustomerMessageSender.Staff => "Admin",
        CustomerMessageSender.Ai => "AI",
        _ => "Tin nhắn",
    };

    private static string GetSenderName(
        CustomerMessageSender sender,
        string? customerName,
        string? staffName,
        string? aiModel) => sender switch
    {
        CustomerMessageSender.Customer => string.IsNullOrWhiteSpace(customerName)
            ? "Bạn"
            : customerName.Trim(),
        CustomerMessageSender.Staff => string.IsNullOrWhiteSpace(staffName)
            ? "Admin"
            : staffName.Trim(),
        CustomerMessageSender.Ai => string.IsNullOrWhiteSpace(aiModel)
            ? "AI"
            : $"AI ({aiModel})",
        _ => "TechStore",
    };

    private static string FormatDate(DateTime value)
    {
        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(
            value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime(), 
            vnZone);
        return vnTime.ToString("dd/MM/yyyy HH:mm", ViCulture);
    }
}
