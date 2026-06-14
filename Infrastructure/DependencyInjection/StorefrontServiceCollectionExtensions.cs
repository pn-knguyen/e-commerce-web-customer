using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Product;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Account.Mock;
using e_commerce_web_customer.Infrastructure.Cart.Db;
using e_commerce_web_customer.Infrastructure.Cart.Mock;
using e_commerce_web_customer.Infrastructure.Catalog.Db;
using e_commerce_web_customer.Infrastructure.Catalog.Mock;
using e_commerce_web_customer.Infrastructure.Home.Db;
using e_commerce_web_customer.Infrastructure.Home.Mock;
using e_commerce_web_customer.Infrastructure.Navigation.Mock;
using e_commerce_web_customer.Infrastructure.Products.Db;
using e_commerce_web_customer.Infrastructure.Products.Mock;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Repositories;
using e_commerce_web_customer.Services;

namespace e_commerce_web_customer.Infrastructure.DependencyInjection;

public static class StorefrontServiceCollectionExtensions
{
    public static IServiceCollection AddMockStorefrontServices(
        this IServiceCollection services)
    {
        services.AddSingleton<ISiteCategoryMenuProvider, MockSiteCategoryMenuProvider>();
        services.AddScoped<IHomePageViewModelFactory, MockHomePageViewModelFactory>();
        services.AddSingleton<IProductDetailViewModelFactory, MockProductDetailViewModelFactory>();
        services.AddScoped<ICatalogPageViewModelFactory, MockCatalogPageViewModelFactory>();
        services.AddScoped<IAuthService, MockAuthService>();
        services.AddScoped<ICartItemValidator, MockCartItemValidator>();

        return services;
    }

    public static IServiceCollection AddDatabaseStorefrontServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.AddSingleton<ISiteCategoryMenuProvider, MockSiteCategoryMenuProvider>();
        services.AddDbContext<EcommerceDbContext>();
        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IHomePageViewModelFactory, DbHomePageViewModelFactory>();
        services.AddScoped<IProductDetailViewModelFactory, DbProductDetailViewModelFactory>();
        services.AddScoped<ICatalogPageViewModelFactory, DbCatalogPageViewModelFactory>();
        services.AddScoped<ICartItemValidator, DbCartItemValidator>();

        return services;
    }
}
