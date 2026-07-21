using e_commerce_web_customer.Application.Contracts;

namespace e_commerce_web_customer.Infrastructure.Integrations.Gemini;

public sealed class MockAiService : IAiService
{
    public Task<AiChatResult> AskAsync(
        string question,
        IReadOnlyList<AiChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        _ = question;
        _ = history;
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AiChatResult(
            "Chat AI đang chạy ở chế độ mock. Bạn có thể chuyển sang DB thật để nhận gợi ý sản phẩm thực tế.",
            []));
    }
}
