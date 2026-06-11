using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.ViewModels.Home;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Application.Home;

/// <summary>
/// Lớp chịu trách nhiệm lấy dữ liệu trang chủ thực tế từ Database.
/// Lập trình viên backend sẽ kết nối DbContext / Repository vào đây.
/// </summary>
public sealed class DbHomePageViewModelFactory : IHomePageViewModelFactory
{
    private readonly ISiteCategoryMenuProvider _categoryMenuProvider;

    public DbHomePageViewModelFactory(ISiteCategoryMenuProvider categoryMenuProvider)
    {
        _categoryMenuProvider = categoryMenuProvider;
    }

    // private readonly MyDbContext _dbContext; // Ví dụ tích hợp DB

    // public DbHomePageViewModelFactory(MyDbContext dbContext)
    // {
    //     _dbContext = dbContext;
    // }

    public HomeIndexViewModel Create()
    {
        // TODO: Viết code truy vấn dữ liệu từ database ở đây
        // Trả về dữ liệu trống hợp lệ (khai báo các trường required) để tránh lỗi biên dịch.
        return new HomeIndexViewModel
        {
            Hero = new HomeHeroViewModel
            {
                Categories = _categoryMenuProvider.GetMenu().Items,
                CampaignTabs = [],
                Slides = [],
                PromoTiles = [],
                BenefitGroups = []
            },
            FeaturedCategorySections = [],
            AccessoryDirectory = new CategoryDirectoryViewModel
            {
                Id = "db-accessory-directory",
                Title = "Phụ kiện từ Database (Chưa tích hợp)",
                ViewAllUrl = "",
                Items = []
            },
            AdditionalCategorySections = []
        };
    }
}
