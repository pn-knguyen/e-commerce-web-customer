namespace e_commerce_web_customer.ViewModels.Shared;

public sealed class QuestionAnswerSectionViewModel
{
    public string SectionId { get; init; } = "block-comment-cps";
    public required string Title { get; init; }
    public required string FormTitle { get; init; }
    public required string Description { get; init; }
    public required string Placeholder { get; init; }
    public required string SubmitLabel { get; init; }
    public int VisibleThreadLimit { get; init; } = 3;
    public int AdditionalCommentCount { get; init; }
    public required IReadOnlyList<QuestionThreadViewModel> Threads { get; init; }
}

public sealed class QuestionThreadViewModel
{
    public required string Author { get; init; }
    public required string Initial { get; init; }
    public required string TimeAgo { get; init; }
    public required string Question { get; init; }
    public required IReadOnlyList<QuestionReplyViewModel> Replies { get; init; }
}

public sealed class QuestionReplyViewModel
{
    public required string Author { get; init; }
    public required string Badge { get; init; }
    public required string TimeAgo { get; init; }
    public required string Content { get; init; }
}
