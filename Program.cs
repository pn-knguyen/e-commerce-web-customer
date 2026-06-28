using System.Text;
using e_commerce_web_customer.Application.CustomerMessages;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Infrastructure.DependencyInjection;
using e_commerce_web_customer.Infrastructure.Web;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AiChatLimiter", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var projectId = builder.Configuration["Firebase:ProjectId"];
if (!string.IsNullOrEmpty(projectId))
{
    // Kiểm tra xem file key có tồn tại không để tránh lỗi crash lúc khởi động
    var keyPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-admin-key.json");
    if (File.Exists(keyPath))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(keyPath),
            ProjectId = projectId
        });
    }
    else
    {
        Console.WriteLine("\n[WARNING] Khong tim thay file firebase-admin-key.json. FirebaseAdmin chua duoc khoi tao!\n");
    }
}

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
builder.Services
    .AddOptions<CustomerMessageJwtOptions>()
    .Bind(builder.Configuration.GetSection(CustomerMessageJwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
        "CustomerMessages:Jwt:Issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.AccessAudience),
        "CustomerMessages:Jwt:AccessAudience is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.AiReceiptAudience),
        "CustomerMessages:Jwt:AiReceiptAudience is required.")
    .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) >= 32,
        "CustomerMessages:Jwt:SigningKey must be at least 32 bytes.")
    .ValidateOnStart();
builder.Services.AddSingleton<ICustomerMessageTokenService, CustomerMessageTokenService>();
builder.Services.AddStorefrontIntegrations(builder.Configuration);

var useMockData = builder.Configuration.GetValue<bool>("DatabaseSettings:UseMockData", true);
if (useMockData)
{
    builder.Services.AddMockStorefrontServices();
}
else
{
    builder.Services.AddDatabaseStorefrontServices(builder.Configuration);
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
app.UseRateLimiter();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
