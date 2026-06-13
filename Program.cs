using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Home;
using e_commerce_web_customer.Application.Navigation;
using e_commerce_web_customer.Application.Product;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Infrastructure.MockData;
using e_commerce_web_customer.Infrastructure.Services;
using e_commerce_web_customer.Infrastructure.Web;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.Repositories;
using e_commerce_web_customer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EcommerceDbContext>();
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISiteCategoryMenuProvider, MockSiteCategoryMenuProvider>();

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
    builder.Services.AddScoped<IHomePageViewModelFactory, MockHomePageViewModelFactory>();
    builder.Services.AddSingleton<IProductDetailViewModelFactory, MockProductDetailViewModelFactory>();
    builder.Services.AddScoped<IAccountService, MockAccountService>();
    builder.Services.AddScoped<ICartItemValidator, MockCartItemValidator>();
}
else
{
    builder.Services.AddScoped<IHomePageViewModelFactory, MockHomePageViewModelFactory>();
    builder.Services.AddScoped<IProductDetailViewModelFactory, DbProductDetailViewModelFactory>();
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
