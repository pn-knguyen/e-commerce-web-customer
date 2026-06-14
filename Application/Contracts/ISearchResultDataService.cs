using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.ViewModels.Search;

namespace e_commerce_web_customer.Application.Contracts;

public interface ISearchResultDataService
{
    Task<SearchResultPageViewModel> SearchAsync(
        SearchResultRequest request,
        CancellationToken cancellationToken = default);
}
