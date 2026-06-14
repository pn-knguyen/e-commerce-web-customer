using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Models;

namespace e_commerce_web_customer.Controllers;

public class HomeController : Controller
{
    private readonly IHomePageViewModelFactory _homePageViewModelFactory;

    public HomeController(IHomePageViewModelFactory homePageViewModelFactory)
    {
        _homePageViewModelFactory = homePageViewModelFactory;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _homePageViewModelFactory.CreateAsync(cancellationToken);
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
