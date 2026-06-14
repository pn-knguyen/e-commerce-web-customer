using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.ViewComponents;

public class HeaderViewComponent : ViewComponent
{
    private readonly ISiteCategoryMenuProvider _categoryMenuProvider;
    private readonly ISearchSuggestionProvider _searchSuggestionProvider;
    private readonly IHeaderAccountProvider _headerAccountProvider;
    private readonly CartSessionService _cartSessionService;

    public HeaderViewComponent(
        ISiteCategoryMenuProvider categoryMenuProvider,
        ISearchSuggestionProvider searchSuggestionProvider,
        IHeaderAccountProvider headerAccountProvider,
        CartSessionService cartSessionService)
    {
        _categoryMenuProvider = categoryMenuProvider;
        _searchSuggestionProvider = searchSuggestionProvider;
        _headerAccountProvider = headerAccountProvider;
        _cartSessionService = cartSessionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var items = _cartSessionService.Load();
        var count = items.Sum(i => Math.Max(1, i.Quantity));
        var isLoggedIn = HttpContext.Session.GetString(SessionKeys.IsLoggedIn) == "true";
        var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
        var userDisplayName = HttpContext.Session.GetString(SessionKeys.UserDisplayName);
        var search = await _searchSuggestionProvider.GetInitialSuggestionsAsync(
            cancellationToken);
        var categoryMenu = await _categoryMenuProvider.GetMenuAsync(
            cancellationToken);
        var account = await _headerAccountProvider.GetAccountAsync(
            isLoggedIn,
            userEmail,
            userDisplayName,
            cancellationToken);
        var currentQuery = HttpContext.Request.Query["q"].ToString();

        return View(new HeaderViewModel
        {
            CategoryMenu = categoryMenu,
            Search = new HeaderSearchViewModel
            {
                Query = currentQuery,
                RecentSearches = search.RecentSearches,
                TrendingSearches = search.TrendingSearches,
                EmptyLogoUrl = search.EmptyLogoUrl
            },
            Account = account,
            CartItemCount = count,
            IsLoggedIn = isLoggedIn
        });
    }
}
