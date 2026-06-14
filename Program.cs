using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Application.Catalog;
using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Product;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.Web;
using e_commerce_web_customer.Infrastructure.Services;
using e_commerce_web_customer.Infrastructure.MockData;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session support — used to pass cart data from Cart → Checkout
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionStorage, WebSessionStorage>();
builder.Services.AddScoped<CartSessionService>();

var useMockData = builder.Configuration.GetValue<bool>("DatabaseSettings:UseMockData", true);
if (useMockData)
{
    builder.Services.AddSingleton<IProductCatalog, MockProductCatalog>();
    builder.Services.AddSingleton<ISiteCategoryMenuProvider, MockSiteCategoryMenuProvider>();
    builder.Services.AddSingleton<IProductDetailViewModelFactory, MockProductDetailViewModelFactory>();
    builder.Services.AddSingleton<ICategoryPageViewModelFactory, MockCategoryPageViewModelFactory>();
    builder.Services.AddSingleton<IHomePageViewModelFactory, MockHomePageViewModelFactory>();
    builder.Services.AddSingleton<ISearchSuggestionProvider, MockSearchSuggestionProvider>();
    builder.Services.AddSingleton<ISearchResultProvider, MockSearchResultProvider>();
    builder.Services.AddSingleton<IHeaderAccountProvider, MockHeaderAccountProvider>();
    builder.Services.AddSingleton<ICartDemoDataProvider, MockCartDemoDataProvider>();
    builder.Services.AddSingleton<IOrderService, MockOrderService>();
    builder.Services.AddScoped<IAccountService, MockAccountService>();
    builder.Services.AddScoped<ICartItemValidator, MockCartItemValidator>();
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is required when UseMockData is false.");
    }

    builder.Services.AddDbContext<EcommerceDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddScoped<IProductCatalog, DbProductCatalog>();
    builder.Services.AddScoped<ISiteCategoryMenuDataService, DbSiteCategoryMenuDataService>();
    builder.Services.AddScoped<IHomePageDataService, DbHomePageDataService>();
    builder.Services.AddScoped<ICategoryPageDataService, DbCategoryPageDataService>();
    builder.Services.AddScoped<IProductDetailDataService, DbProductDetailDataService>();
    builder.Services.AddScoped<ISearchSuggestionDataService, DbSearchSuggestionDataService>();
    builder.Services.AddScoped<ISearchResultDataService, DbSearchResultDataService>();
    builder.Services.AddScoped<IHeaderAccountDataService, DbHeaderAccountDataService>();
    builder.Services.AddScoped<ISiteCategoryMenuProvider, DbSiteCategoryMenuProvider>();
    builder.Services.AddScoped<IProductDetailViewModelFactory, DbProductDetailViewModelFactory>();
    builder.Services.AddScoped<ICategoryPageViewModelFactory, DbCategoryPageViewModelFactory>();
    builder.Services.AddScoped<IHomePageViewModelFactory, DbHomePageViewModelFactory>();
    builder.Services.AddScoped<ISearchSuggestionProvider, DbSearchSuggestionProvider>();
    builder.Services.AddScoped<ISearchResultProvider, DbSearchResultProvider>();
    builder.Services.AddScoped<IHeaderAccountProvider, DbHeaderAccountProvider>();
    builder.Services.AddScoped<ICartDemoDataProvider, EmptyCartDemoDataProvider>();
    builder.Services.AddScoped<IOrderService, DbOrderService>();
    builder.Services.AddScoped<IAccountService, DbAccountService>();
    builder.Services.AddScoped<ICartItemValidator, DbCartItemValidator>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
