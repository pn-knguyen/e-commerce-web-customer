using e_commerce_web_customer.Application.Search;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

[Route("search")]
public sealed class SearchController(
    ISearchSuggestionProvider searchSuggestionProvider,
    ISearchResultProvider searchResultProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? q,
        [FromQuery] string? sort,
        [FromQuery] string? category,
        [FromQuery] int page,
        CancellationToken cancellationToken)
    {
        var model = await searchResultProvider.SearchAsync(
            new SearchResultRequest(q, sort, category, page),
            cancellationToken);

        return View(model);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(
        [FromQuery] string? q,
        [FromQuery] string? sort,
        [FromQuery] string? category,
        [FromQuery] int page,
        CancellationToken cancellationToken)
    {
        var model = await searchResultProvider.SearchAsync(
            new SearchResultRequest(q, sort, category, page),
            cancellationToken);

        return PartialView("~/Views/Search/_ProductGridItems.cshtml", model);
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var model = await searchSuggestionProvider.SearchAsync(
            q,
            cancellationToken);
        return Json(model);
    }
}
