using e_commerce_web_customer.ViewModels.Home;

namespace e_commerce_web_customer.Application.Home;

public interface IHomePageViewModelFactory
{
    Task<HomeIndexViewModel> CreateAsync();
}
