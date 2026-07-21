namespace e_commerce_web_customer.ViewModels.Shared;

public sealed record StaticInfoPageViewModel(
    string Slug,
    string Title,
    string Eyebrow,
    string Description,
    IReadOnlyList<string> Highlights,
    string PrimaryActionLabel,
    string PrimaryActionUrl);
