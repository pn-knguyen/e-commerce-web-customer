using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.DTOs.Product;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

public class CatalogController(ICatalogPageViewModelFactory pageViewModelFactory) : Controller
{
    [HttpGet("/catalog")]
    public async Task<IActionResult> Index([FromQuery] ProductQuery query)
    {
        return View(await pageViewModelFactory.CreateAsync(query));
    }
}
