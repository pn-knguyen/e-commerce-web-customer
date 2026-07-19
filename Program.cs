using System.Security.Claims;
using System.Text;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.CustomerMessages;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Infrastructure.DependencyInjection;
using e_commerce_web_customer.Infrastructure.Web;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

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
    var keyPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-admin-key.json");
    var firebaseJsonVar = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_KEY");

    if (!string.IsNullOrWhiteSpace(firebaseJsonVar))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory
                .FromJson<ServiceAccountCredential>(firebaseJsonVar)
                .ToGoogleCredential(),
            ProjectId = projectId
        });
        Console.WriteLine("\n[INFO] Da khoi tao FirebaseAdmin tu Environment Variable.\n");
    }
    else if (File.Exists(keyPath))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory
                .FromFile<ServiceAccountCredential>(keyPath)
                .ToGoogleCredential(),
            ProjectId = projectId
        });
        Console.WriteLine("\n[INFO] Da khoi tao FirebaseAdmin tu file firebase-admin-key.json.\n");
    }
    else
    {
        Console.WriteLine("\n[WARNING] Khong tim thay FIREBASE_ADMIN_KEY hoac file firebase-admin-key.json. FirebaseAdmin chua duoc khoi tao!\n");
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
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TechStore.Customer.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
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
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var versioned = context.Context.Request.Query.ContainsKey("v");
        context.Context.Response.Headers.CacheControl = versioned
            ? "public,max-age=31536000,immutable"
            : "public,max-age=3600";
    }
});
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    SyncAuthenticatedCustomerSession(context);
    await next();
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static void SyncAuthenticatedCustomerSession(HttpContext context)
{
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        return;
    }

    SetSessionValue(context, SessionKeys.IsLoggedIn, "true");
    SetSessionValue(
        context,
        SessionKeys.UserEmail,
        context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier));
    SetSessionValue(context, SessionKeys.UserDisplayName, context.User.FindFirstValue(ClaimTypes.Name));
    SetSessionValue(context, SessionKeys.UserPhoneNumber, context.User.FindFirstValue(ClaimTypes.MobilePhone));
}

static void SetSessionValue(HttpContext context, string key, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        if (key == SessionKeys.UserPhoneNumber)
        {
            context.Session.Remove(key);
        }

        return;
    }

    var normalized = value.Trim();
    if (!string.Equals(context.Session.GetString(key), normalized, StringComparison.Ordinal))
    {
        context.Session.SetString(key, normalized);
    }
}
