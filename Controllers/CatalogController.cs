using e_commerce_web_customer.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

[Route("catalog")]
public sealed class CatalogController(ICategoryPageViewModelFactory categoryPageFactory) : Controller
{
    [HttpGet("")]
    public Task<IActionResult> Index(
        [FromQuery] string? cat,
        [FromQuery] string? brand,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        return RenderCategoryAsync(cat, brand, sort, cancellationToken);
    }

    [HttpGet("{slug}")]
    public Task<IActionResult> Category(
        string slug,
        [FromQuery] string? brand,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        return RenderCategoryAsync(slug, brand, sort, cancellationToken);
    }

    private async Task<IActionResult> RenderCategoryAsync(
        string? slug,
        string? brand,
        string? sort,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = string.IsNullOrWhiteSpace(slug)
            ? "phone"
            : slug.Trim().ToLowerInvariant();

        var model = await categoryPageFactory.CreateAsync(
            new CategoryPageRequest(normalizedSlug, brand, sort),
            cancellationToken);

        return model is null ? NotFound() : View("Category", model);
    }
}
