using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Controllers;

public sealed class AccountController(
    IAccountService accountService,
    IAccountProfilePageProvider accountProfilePageProvider,
    IAccountOrderDetailProvider accountOrderDetailProvider,
    IAccountAddressService accountAddressService,
    CartSessionService cartSession) : Controller
{
    private static readonly MemoryCache MagicLinkSessions = new MemoryCache(new MemoryCacheOptions());

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        cartSession.Clear();
        cartSession.ClearBuyNow();
        HttpContext.Session.Remove(SessionKeys.IsLoggedIn);
        HttpContext.Session.Remove(SessionKeys.UserEmail);
        HttpContext.Session.Remove(SessionKeys.UserDisplayName);
        HttpContext.Session.Remove(SessionKeys.UserPhoneNumber);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile(
        string? tab = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(
                nameof(Profile),
                "Account",
                new { tab = AccountProfileTabs.Normalize(tab) });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var model = await accountProfilePageProvider.GetProfilePageAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            HttpContext.Session.GetString(SessionKeys.UserDisplayName),
            HttpContext.Session.GetString(SessionKeys.UserPhoneNumber),
            AccountProfileTabs.Normalize(tab),
            cancellationToken);

        return View(model);
    }

    [HttpGet("account/orders/{code}")]
    public async Task<IActionResult> OrderDetail(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(
                nameof(OrderDetail),
                "Account",
                new { code });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var model = await accountOrderDetailProvider.GetOrderDetailAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            HttpContext.Session.GetString(SessionKeys.UserDisplayName),
            HttpContext.Session.GetString(SessionKeys.UserPhoneNumber),
            code,
            cancellationToken);

        return model is null
            ? NotFound()
            : View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(
        AccountAddressFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.Info });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Vui lòng kiểm tra lại thông tin địa chỉ.";
            return RedirectToProfileInfo();
        }

        var result = await accountAddressService.AddAddressAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            ToAddressInput(model),
            cancellationToken);

        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Message;
        return RedirectToProfileInfo();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(
        long addressId,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.Info });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var result = await accountAddressService.SetDefaultAddressAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            addressId,
            cancellationToken);

        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Message;
        return RedirectToProfileInfo();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? source = null)
    {
        var providerName = provider?.ToLowerInvariant() switch
        {
            "google" => "Google",
            "facebook" => "Facebook",
            _ => null
        };

        if (providerName is null)
        {
            return BadRequest();
        }

        TempData["AuthNotice"] =
            $"Kết nối {providerName} cần được cấu hình trước khi sử dụng đăng nhập mạng xã hội.";

        return string.Equals(source, nameof(Register), StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(Register))
            : RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> FirebaseSync([FromBody] FirebaseSyncRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { success = false, message = "Token is required." });
        }

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            MagicLinkSessions.Set(request.SessionId, request.IdToken, TimeSpan.FromMinutes(15));
        }

        return await ProcessFirebaseLoginAsync(request.IdToken, request.ReturnUrl, request.DisplayName, request.PhoneNumber, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> PollMagicLinkSession(string sessionId, string? returnUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !MagicLinkSessions.TryGetValue(sessionId, out string? idToken) || idToken == null)
        {
            return Json(new { success = false, message = "Đang chờ xác thực..." });
        }

        // Token found, process login
        MagicLinkSessions.Remove(sessionId);

        return await ProcessFirebaseLoginAsync(idToken, returnUrl, null, null, cancellationToken);
    }

    private async Task<IActionResult> ProcessFirebaseLoginAsync(string idToken, string? returnUrl, string? fallbackDisplayName, string? fallbackPhoneNumber, CancellationToken cancellationToken)
    {
        FirebaseToken decodedToken;
        try
        {
            decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Invalid Token: " + ex.Message });
        }

        var uid = decodedToken.Uid;
        UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken);
        
        var tokenName = userRecord.DisplayName;
        var tokenPhone = userRecord.PhoneNumber;

        var finalDisplayName = tokenName ?? fallbackDisplayName;
        var finalPhoneNumber = tokenPhone ?? fallbackPhoneNumber;

        string email;
        if (!string.IsNullOrWhiteSpace(userRecord.Email))
        {
            email = userRecord.Email;
        }
        else
        {
            // Nếu người dùng không có email từ Firebase (VD: Đăng nhập bằng số điện thoại),
            // ta kiểm tra xem số điện thoại này đã được gắn với tài khoản nào chưa.
            string? existingEmail = null;
            if (!string.IsNullOrWhiteSpace(finalPhoneNumber))
            {
                existingEmail = await accountService.FindEmailByPhoneNumberAsync(finalPhoneNumber, cancellationToken);
            }

            email = existingEmail ?? $"{uid}@techstore.local";
        }

        var exists = await accountService.UserExistsAsync(email, cancellationToken);
        if (!exists)
        {
            var registerModel = new RegisterViewModel
            {
                Email = email,
                FullName = string.IsNullOrWhiteSpace(finalDisplayName) 
                    ? (string.IsNullOrWhiteSpace(finalPhoneNumber) ? ResolveDisplayName(email) : finalPhoneNumber) 
                    : finalDisplayName,
                PhoneNumber = finalPhoneNumber,
                Password = Guid.NewGuid().ToString() + "A1!",
                ConfirmPassword = string.Empty,
                AgreeToTerms = true
            };
            registerModel.ConfirmPassword = registerModel.Password;
            var success = await accountService.RegisterAsync(registerModel, cancellationToken);
            if (!success)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: Không thể tạo hồ sơ người dùng." });
            }
        }

        var profile = await accountService.GetProfileAsync(email, cancellationToken);
        if (profile == null)
        {
             return Json(new { success = false, message = "Lỗi hệ thống: Hồ sơ người dùng không tồn tại." });
        }

        HttpContext.Session.SetString(SessionKeys.IsLoggedIn, "true");
        HttpContext.Session.SetString(SessionKeys.UserEmail, profile?.Email ?? email);
        HttpContext.Session.SetString(
            SessionKeys.UserDisplayName,
            profile?.DisplayName ?? finalDisplayName ?? finalPhoneNumber ?? ResolveDisplayName(email));
        
        if (!string.IsNullOrWhiteSpace(profile?.PhoneNumber ?? finalPhoneNumber))
        {
            HttpContext.Session.SetString(SessionKeys.UserPhoneNumber, profile?.PhoneNumber ?? finalPhoneNumber!);
        }

        return Json(new { 
            success = true, 
            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")
        });
    }

    private static string ResolveDisplayName(string email)
    {
        return email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? email;
    }

    private bool IsLoggedIn()
    {
        return HttpContext.Session.GetString(SessionKeys.IsLoggedIn) == "true";
    }

    private static AccountAddressInput ToAddressInput(AccountAddressFormViewModel model) => new(
        model.ContactName,
        model.Phone,
        model.ProvinceCode,
        model.ProvinceName,
        model.DistrictCode,
        model.DistrictName,
        model.WardCode,
        model.WardName,
        model.DetailAddress,
        model.IsDefault);

    private IActionResult RedirectToProfileInfo()
    {
        var url = Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.Info })
            ?? "/Account/Profile?tab=info";
        return Redirect(url + "#profile-address-title");
    }
}
