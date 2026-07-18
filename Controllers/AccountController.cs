using System.Security.Claims;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_customer.Controllers;

public sealed class AccountController(
    IAccountService accountService,
    IAccountProfilePageProvider accountProfilePageProvider,
    IAccountOrderDetailProvider accountOrderDetailProvider,
    IAccountAddressService accountAddressService,
    IOrderReviewService orderReviewService,
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
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        cartSession.Clear();
        cartSession.ClearBuyNow();
        cartSession.ClearCheckoutSelection();
        HttpContext.Session.Remove(SessionKeys.IsLoggedIn);
        HttpContext.Session.Remove(SessionKeys.UserEmail);
        HttpContext.Session.Remove(SessionKeys.UserDisplayName);
        HttpContext.Session.Remove(SessionKeys.UserPhoneNumber);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile(
        string? tab = null,
        string? status = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(
                nameof(Profile),
                "Account",
                new
                {
                    tab = AccountProfileTabs.Normalize(tab),
                    status,
                    from,
                    to
                });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var model = await accountProfilePageProvider.GetProfilePageAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            HttpContext.Session.GetString(SessionKeys.UserDisplayName),
            HttpContext.Session.GetString(SessionKeys.UserPhoneNumber),
            AccountProfileTabs.Normalize(tab),
            status,
            from,
            to,
            cancellationToken);

        return View(model);
    }

    [HttpGet("account/profile/order-history")]
    public async Task<IActionResult> ProfileOrderHistory(
        string? status = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return Unauthorized();
        }

        var model = await accountProfilePageProvider.GetOrderHistoryAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            status,
            from,
            to,
            cancellationToken);

        Response.Headers.CacheControl = "no-store, no-cache";
        return PartialView("_AccountOrderHistory", model);
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

    [HttpPost("account/orders/reviews")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(
        AccountOrderReviewFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = BuildOrderDetailUrl(model.OrderCode);
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        if (!ModelState.IsValid)
        {
            TempData["ReviewError"] = "Vui lòng chọn số sao từ 1 đến 5 và kiểm tra lại nội dung đánh giá.";
            return RedirectToOrderDetailReview(model.OrderCode);
        }

        var result = await orderReviewService.SubmitReviewAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            new OrderReviewInput(model.OrderItemId, model.Stars, model.Comment),
            cancellationToken);

        TempData[result.Success ? "ReviewSuccess" : "ReviewError"] = result.Message;
        return RedirectToOrderDetailReview(model.OrderCode);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        AccountProfileUpdateViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            var returnUrl = Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.Info });
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Vui lòng kiểm tra lại thông tin cá nhân.";
            return RedirectToProfilePersonalInfo();
        }

        var result = await accountService.UpdateProfileAsync(
            HttpContext.Session.GetString(SessionKeys.UserEmail),
            new AccountProfileUpdateInput(
                model.FullName,
                model.PhoneNumber,
                model.Gender),
            cancellationToken);

        if (result.Success && result.Profile is not null)
        {
            SetLoginSession(result.Profile.Email, result.Profile.DisplayName, result.Profile.PhoneNumber);
            await SignInCustomerAsync(result.Profile.Email, result.Profile.DisplayName, result.Profile.PhoneNumber);
        }

        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Message;
        return RedirectToProfilePersonalInfo();
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

        var resolvedEmail = profile.Email;
        var resolvedDisplayName = profile.DisplayName;
        var resolvedPhoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber)
            ? finalPhoneNumber
            : profile.PhoneNumber;

        SetLoginSession(resolvedEmail, resolvedDisplayName, resolvedPhoneNumber);
        await SignInCustomerAsync(resolvedEmail, resolvedDisplayName, resolvedPhoneNumber);

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
        return HttpContext.User?.Identity?.IsAuthenticated == true
            || HttpContext.Session.GetString(SessionKeys.IsLoggedIn) == "true";
    }

    private void SetLoginSession(string email, string displayName, string? phoneNumber)
    {
        HttpContext.Session.SetString(SessionKeys.IsLoggedIn, "true");
        HttpContext.Session.SetString(SessionKeys.UserEmail, email);
        HttpContext.Session.SetString(SessionKeys.UserDisplayName, displayName);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            HttpContext.Session.Remove(SessionKeys.UserPhoneNumber);
            return;
        }

        HttpContext.Session.SetString(SessionKeys.UserPhoneNumber, phoneNumber.Trim());
    }

    private async Task SignInCustomerAsync(string email, string displayName, string? phoneNumber)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName)
        };

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, phoneNumber.Trim()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
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

    private IActionResult RedirectToProfilePersonalInfo()
    {
        var url = Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.Info })
            ?? "/Account/Profile?tab=info";
        return Redirect(url + "#profile-info-title");
    }

    private IActionResult RedirectToOrderDetailReview(string? orderCode)
    {
        var url = BuildOrderDetailUrl(orderCode);
        if (string.IsNullOrWhiteSpace(url))
        {
            return RedirectToAction(nameof(Profile), new { tab = AccountProfileTabs.History });
        }

        return Redirect(url + "#order-reviews");
    }

    private string? BuildOrderDetailUrl(string? orderCode)
    {
        var normalizedCode = orderCode?.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return Url.Action(nameof(Profile), "Account", new { tab = AccountProfileTabs.History });
        }

        return Url.Action(nameof(OrderDetail), "Account", new { code = normalizedCode })
            ?? "/account/orders/" + Uri.EscapeDataString(normalizedCode);
    }
}
