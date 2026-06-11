using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.ViewComponents;

public class HeaderViewComponent : ViewComponent
{
    private readonly ISiteCategoryMenuProvider _categoryMenuProvider;

    public HeaderViewComponent(ISiteCategoryMenuProvider categoryMenuProvider)
    {
        _categoryMenuProvider = categoryMenuProvider;
    }

    public IViewComponentResult Invoke()
    {
        return View(new HeaderViewModel
        {
            CategoryMenu = _categoryMenuProvider.GetMenu()
        });
    }
}
