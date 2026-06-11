namespace e_commerce_web_customer.ViewModels.Shared;

public sealed class CategoryDirectoryViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string ViewAllUrl { get; init; }
    public IReadOnlyList<CategoryDirectoryLinkViewModel> HeaderLinks { get; init; } = [];
    public required IReadOnlyList<CategoryDirectoryItemViewModel> Items { get; init; }
}

public sealed class CategoryDirectoryLinkViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
}

public sealed class CategoryDirectoryItemViewModel
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public required string ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
}
