using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.ViewModels.Catalog;
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
        [FromQuery] bool inStock,
        [FromQuery] bool isNew,
        [FromQuery] int page,
        CancellationToken cancellationToken)
    {
        return RenderCategoryAsync(cat, brand, sort, inStock, isNew, page, cancellationToken);
    }

    [HttpGet("{slug}")]
    public Task<IActionResult> Category(
        string slug,
        [FromQuery] string? brand,
        [FromQuery] string? sort,
        [FromQuery] bool inStock,
        [FromQuery] bool isNew,
        [FromQuery] int page,
        CancellationToken cancellationToken)
    {
        return RenderCategoryAsync(slug, brand, sort, inStock, isNew, page, cancellationToken);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(
        [FromQuery] string? cat,
        [FromQuery] string? brand,
        [FromQuery] string? sort,
        [FromQuery] bool inStock,
        [FromQuery] bool isNew,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = string.IsNullOrWhiteSpace(cat)
            ? "phone"
            : cat.Trim().ToLowerInvariant();
        var model = await categoryPageFactory.CreateAsync(
            new CategoryPageRequest(
                normalizedSlug,
                brand,
                sort,
                GetCatalogFiltersFromQuery(),
                inStock,
                isNew,
                page),
            cancellationToken);

        return model is null
            ? NotFound()
            : PartialView("~/Views/Catalog/Partials/_ProductGridItems.cshtml", model);
    }

    [HttpGet("section-products")]
    public async Task<IActionResult> SectionProducts(
        [FromQuery] string? cat,
        [FromQuery] string? brand,
        [FromQuery] string? sort,
        [FromQuery] bool inStock,
        [FromQuery] bool isNew,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cat))
        {
            return BadRequest();
        }

        var normalizedSlug = cat.Trim().ToLowerInvariant();
        var model = await categoryPageFactory.CreateAsync(
            new CategoryPageRequest(
                normalizedSlug,
                brand,
                sort,
                GetCatalogFiltersFromQuery(),
                inStock,
                isNew),
            cancellationToken);

        if (model is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";

        return PartialView(
            "~/Views/Catalog/Partials/_SectionProductPanel.cshtml",
            new CategoryProductSectionViewModel
            {
                Id = $"section-products-{model.Slug}",
                Title = model.Title,
                ViewAllUrl = BuildCatalogUrl(model.Slug, brand, sort),
                VisibleProductLimit = 20,
                Products = model.Products
            });
    }

    private async Task<IActionResult> RenderCategoryAsync(
        string? slug,
        string? brand,
        string? sort,
        bool inStock,
        bool isNew,
        int page,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = string.IsNullOrWhiteSpace(slug)
            ? "phone"
            : slug.Trim().ToLowerInvariant();

        var filters = GetCatalogFiltersFromQuery();

        var model = await categoryPageFactory.CreateAsync(
            new CategoryPageRequest(
                normalizedSlug,
                brand,
                sort,
                filters,
                inStock,
                isNew,
                page),
            cancellationToken);

        return model is null ? NotFound() : View("Category", model);
    }

    private IReadOnlyDictionary<string, IReadOnlyList<string>> GetCatalogFiltersFromQuery()
    {
        return Request.Query
            .Where(item => item.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                item => item.Key[2..].Trim().ToLowerInvariant(),
                item => (IReadOnlyList<string>)item.Value
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildCatalogUrl(
        string slug,
        string? brand,
        string? sort)
    {
        var query = new List<string>
        {
            $"cat={Uri.EscapeDataString(slug)}"
        };

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query.Add($"brand={Uri.EscapeDataString(brand.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            query.Add($"sort={Uri.EscapeDataString(sort.Trim())}");
        }

        return $"/catalog?{string.Join('&', query)}";
    }
}
