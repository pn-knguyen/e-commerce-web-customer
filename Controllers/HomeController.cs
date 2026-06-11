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

    public IActionResult Index()
    {
        return View(_homePageViewModelFactory.Create());
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
