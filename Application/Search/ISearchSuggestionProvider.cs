using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Search;

public interface ISearchSuggestionProvider
{
    Task<HeaderSearchViewModel> GetInitialSuggestionsAsync(
        CancellationToken cancellationToken = default);

    Task<SearchSuggestionResultsViewModel> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default);
}
