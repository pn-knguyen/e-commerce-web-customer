using System.Text.Json;
using e_commerce_web_customer.Application.Account;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Orders;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.ViewModels.Checkout;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

public sealed class CheckoutController(
    CartSessionService cartSession,
    ICartItemValidator cartItemValidator,
    ICartPersistenceService cartPersistenceService,
    ICartDemoDataProvider demoDataProvider,
    ICheckoutPaymentMethodProvider paymentMethodProvider,
    IOrderService orderService,
    IAccountAddressService accountAddressService,
    IMoMoIntegration momoIntegration,
    IVnPayIntegration vnPayIntegration) : Controller
{
    private const string SuccessSessionKey = "checkout_success_order";

    [HttpGet]
    public async Task<IActionResult> Index(
        bool demo = false,
        string mode = "",
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return RedirectToLogin(mode);
        }

        var model = await BuildModelAsync(demo, mode, cancellationToken);
        if (model.Items.Count == 0 && !demo)
        {
            return RedirectToAction("Index", "Cart");
        }

        ViewData["Mode"] = mode;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        CheckoutViewModel model,
        [FromQuery] string mode = "",
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return RedirectToLogin(mode);
        }

        var orderSnapshot = await BuildModelAsync(
            demo: false,
            mode,
            cancellationToken);

        if (orderSnapshot.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            RestoreOrderSummary(model, orderSnapshot);
            ViewData["Mode"] = mode;
            return View(model);
        }

        try
        {
            var paymentProvider = ResolvePaymentProvider(
                model.PaymentMethodId,
                orderSnapshot.PaymentMethods);
            var isOnlinePayment = paymentProvider.Contains("momo") || paymentProvider.Contains("vnpay") || paymentProvider.Contains("sepay");

            var placedOrder = await orderService.PlaceOrderAsync(
                BuildOrderRequest(model, orderSnapshot),
                clearCart: !isOnlinePayment,
                cancellationToken);
            var successModel = BuildSuccessModel(
                model,
                orderSnapshot,
                placedOrder);

            HttpContext.Session.SetString(
                SuccessSessionKey,
                JsonSerializer.Serialize(successModel));
            
            if (!isOnlinePayment)
            {
                await ClearCompletedCartAsync(mode, cancellationToken);
            }

            return await ProcessPaymentRedirectAsync(
                paymentProvider,
                placedOrder.OrderCode,
                (long)orderSnapshot.Total,
                cancellationToken);
        }
        catch (OrderPlacementException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            RestoreOrderSummary(model, orderSnapshot);
            ViewData["Mode"] = mode;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Success()
    {
        var successModel = ReadSuccessModel();
        if (successModel is not null)
        {
            cartSession.Clear();
            cartSession.ClearBuyNow();
        }

        return successModel is null
            ? RedirectToAction("Index", "Cart")
            : View(successModel);
    }

    private async Task<IActionResult> ProcessPaymentRedirectAsync(
        string paymentProvider,
        string orderCode,
        long totalAmount,
        CancellationToken cancellationToken)
    {
        if (paymentProvider.Contains("momo"))
        {
            var redirectUrl = Url.ActionLink("MoMoReturn", "Payment");
            var ipnUrl = Url.ActionLink("MoMoIpn", "Payment");
            var momoReq = new MoMoCreateRequest(
                OrderId: orderCode,
                Amount: totalAmount,
                OrderInfo: $"Thanh toan don hang {orderCode}",
                RedirectUrl: redirectUrl ?? string.Empty,
                IpnUrl: ipnUrl ?? string.Empty);

            var momoResult = await momoIntegration.CreatePaymentAsync(momoReq, cancellationToken);

            if (momoResult.Success && !string.IsNullOrEmpty(momoResult.PayUrl))
            {
                return Redirect(momoResult.PayUrl);
            }

            TempData["PaymentError"] = momoResult.ErrorMessage ?? "Không thể khởi tạo giao dịch MoMo. Vui lòng thử thanh toán lại.";
            return RedirectToAction("OrderDetail", "Account", new { code = orderCode });
        }

        if (paymentProvider.Contains("vnpay"))
        {
            var returnUrl = Url.ActionLink("VnPayReturn", "Payment");
            var ipnUrl = Url.ActionLink("VnPayIpn", "Payment");
            
            var vnpayReq = new VnPayCreateRequest(
                OrderId: orderCode,
                Amount: totalAmount,
                OrderInfo: $"Thanh toan don hang {orderCode}",
                ReturnUrl: returnUrl ?? string.Empty,
                IpnUrl: ipnUrl ?? string.Empty,
                IpAddress: GetClientIpAddress()
            );

            var vnpayResult = await vnPayIntegration.CreatePaymentUrlAsync(vnpayReq, cancellationToken);

            if (vnpayResult.Success && !string.IsNullOrEmpty(vnpayResult.PayUrl))
            {
                return Redirect(vnpayResult.PayUrl);
            }

            TempData["PaymentError"] = vnpayResult.ErrorMessage ?? "Không thể khởi tạo giao dịch VNPay. Vui lòng thử thanh toán lại.";
            return RedirectToAction("OrderDetail", "Account", new { code = orderCode });
        }

        if (paymentProvider.Contains("sepay"))
        {
            return RedirectToAction(
                "Index",
                "SePayPayment",
                new { orderCode = orderCode });
        }

        return RedirectToAction(nameof(Success));
    }

    private async Task<CheckoutViewModel> BuildModelAsync(
        bool demo,
        string mode,
        CancellationToken cancellationToken)
    {
        var sessionItems = string.Equals(
            mode,
            "buynow",
            StringComparison.OrdinalIgnoreCase)
            ? cartSession.LoadBuyNow()
            : cartSession.Load();

        if (sessionItems.Count == 0
            && !string.Equals(mode, "buynow", StringComparison.OrdinalIgnoreCase)
            && GetLoggedInUserEmail() is { } storedCartEmail)
        {
            sessionItems = await cartPersistenceService.LoadAsync(
                storedCartEmail,
                cancellationToken);
            cartSession.Save(sessionItems);
        }

        IReadOnlyList<CheckoutItemViewModel> items;
        if (sessionItems.Count > 0)
        {
            var validatedItems = new List<CartSessionItem>();
            foreach (var sessionItem in sessionItems)
            {
                try
                {
                    validatedItems.Add(await cartItemValidator.ValidateAsync(
                        sessionItem,
                        cancellationToken));
                }
                catch (CartItemValidationException)
                {
                    // Products that became inactive or out of stock are removed.
                }
            }

            if (string.Equals(mode, "buynow", StringComparison.OrdinalIgnoreCase))
            {
                if (validatedItems.Count > 0)
                {
                    cartSession.SaveBuyNow(validatedItems[0]);
                }
                else
                {
                    cartSession.ClearBuyNow();
                }
            }
            else
            {
                cartSession.Save(validatedItems);
                if (GetLoggedInUserEmail() is { } userEmail)
                {
                    await cartPersistenceService.SaveAsync(
                        userEmail,
                        validatedItems,
                        cancellationToken);
                }
            }

            items = validatedItems.Select(item => new CheckoutItemViewModel
            {
                ProductId = item.Id,
                Name = item.Name,
                ImageUrl = item.ImageUrl,
                ImageAlt = item.ImageAlt,
                Variant = item.Variant,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            }).ToList();
        }
        else if (demo)
        {
            items = await demoDataProvider.GetCheckoutItemsAsync(
                cancellationToken);
        }
        else
        {
            items = [];
        }

        var paymentMethods = await paymentMethodProvider.GetActivePaymentMethodsAsync(
            cancellationToken);

        var model = new CheckoutViewModel
        {
            Items = items,
            PaymentMethods = paymentMethods,
            PaymentMethodId = paymentMethods.FirstOrDefault()?.Id ?? 0,
            ShippingFee = 30_000m,
            Discount = 0m,
            Email = GetLoggedInUserEmail() ?? string.Empty,
            FullName = HttpContext.Session.GetString(SessionKeys.UserDisplayName) ?? string.Empty,
            Phone = HttpContext.Session.GetString(SessionKeys.UserPhoneNumber) ?? string.Empty
        };

        if (GetLoggedInUserEmail() is { } defaultAddressEmail)
        {
            var defaultAddress = await accountAddressService.GetDefaultAddressAsync(
                defaultAddressEmail,
                cancellationToken);
            if (defaultAddress is not null)
            {
                ApplyDefaultAddress(model, defaultAddress);
            }
        }

        return model;
    }

    private PlaceOrderRequest BuildOrderRequest(
        CheckoutViewModel submittedModel,
        CheckoutViewModel order)
    {
        return new PlaceOrderRequest(
            GetRequiredLoggedInUserEmail(),
            submittedModel.FullName.Trim(),
            submittedModel.Phone.Trim(),
            submittedModel.Email.Trim(),
            submittedModel.ShippingAddressId,
            ResolveLocationName(submittedModel.ProvinceName, submittedModel.Province),
            ResolveLocationName(submittedModel.WardName, submittedModel.Ward),
            BuildShippingDetail(submittedModel),
            submittedModel.PaymentMethodId,
            submittedModel.Note?.Trim(),
            order.ShippingFee,
            order.Discount,
            order.Items.Select(item => new PlaceOrderLine(
                item.ProductId,
                item.Name,
                item.Variant,
                item.UnitPrice,
                item.Quantity)).ToList());
    }

    private static void ApplyDefaultAddress(
        CheckoutViewModel model,
        AccountAddressSnapshot address)
    {
        model.ShippingAddressId = address.Id;
        model.FullName = address.ContactName;
        model.Phone = address.Phone;
        model.Province = address.ProvinceCode;
        model.ProvinceName = address.ProvinceName;
        model.District = address.DistrictCode ?? string.Empty;
        model.DistrictName = address.DistrictName ?? string.Empty;
        model.Ward = address.WardCode;
        model.WardName = address.WardName;
        model.AddressDetail = address.DetailAddress;
    }

    private CheckoutSuccessViewModel? ReadSuccessModel()
    {
        var json = HttpContext.Session.GetString(SuccessSessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CheckoutSuccessViewModel>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CheckoutSuccessViewModel BuildSuccessModel(
        CheckoutViewModel submittedModel,
        CheckoutViewModel order,
        PlacedOrder placedOrder)
    {
        return new CheckoutSuccessViewModel
        {
            OrderCode = placedOrder.OrderCode,
            CustomerName = string.IsNullOrWhiteSpace(submittedModel.FullName)
                ? "Quý khách"
                : submittedModel.FullName.Trim(),
            Phone = submittedModel.Phone?.Trim() ?? string.Empty,
            Email = submittedModel.Email?.Trim() ?? string.Empty,
            DeliveryAddress = BuildDeliveryAddress(submittedModel),
            ShippingMethodName = "Giao hàng nhanh",
            PaymentMethodName = GetPaymentMethodName(
                submittedModel.PaymentMethodId,
                order.PaymentMethods),
            PlacedAt = placedOrder.PlacedAt.ToString("HH:mm, dd/MM/yyyy"),
            EstimatedDeliveryDateText =
                placedOrder.EstimatedDeliveryAt.ToString("dd/MM/yyyy"),
            ItemCountText = FormatItemCount(order.Items),
            SubtotalText = CheckoutViewModel.FormatPrice(order.Subtotal),
            ShippingFeeText = CheckoutViewModel.FormatPrice(order.ShippingFee),
            DiscountText = FormatDiscount(order.Discount),
            TotalText = CheckoutViewModel.FormatPrice(order.Total),
            Items = BuildSuccessItems(order.Items)
        };
    }

    private static List<CheckoutSuccessItemViewModel> BuildSuccessItems(
        IEnumerable<CheckoutItemViewModel> items)
    {
        return items.Select(item => new CheckoutSuccessItemViewModel
        {
            Name = item.Name,
            ImageUrl = item.ImageUrl,
            ImageAlt = item.ImageAlt,
            Variant = item.Variant,
            Quantity = item.Quantity,
            LineTotalText = CheckoutViewModel.FormatPrice(
                item.UnitPrice * item.Quantity)
        }).ToList();
    }

    private static void RestoreOrderSummary(
        CheckoutViewModel target,
        CheckoutViewModel source)
    {
        target.Items = source.Items;
        target.PaymentMethods = source.PaymentMethods;
        target.ShippingFee = source.ShippingFee;
        target.Discount = source.Discount;
    }

    private async Task ClearCompletedCartAsync(
        string mode,
        CancellationToken cancellationToken)
    {
        if (string.Equals(mode, "buynow", StringComparison.OrdinalIgnoreCase))
        {
            cartSession.ClearBuyNow();
            return;
        }

        cartSession.Clear();
        if (GetLoggedInUserEmail() is { } userEmail)
        {
            await cartPersistenceService.ClearAsync(
                userEmail,
                cancellationToken);
        }
    }

    private bool IsLoggedIn()
    {
        return HttpContext.Session.GetString(SessionKeys.IsLoggedIn) == "true";
    }

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
        {
            ip = ip.Split(',')[0].Trim();
        }
        
        if (string.IsNullOrEmpty(ip))
        {
            ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        if (string.IsNullOrEmpty(ip) || ip == "::1")
        {
            ip = "127.0.0.1";
        }

        return ip;
    }

    private string? GetLoggedInUserEmail()
    {
        if (!IsLoggedIn())
        {
            return null;
        }

        var email = HttpContext.Session.GetString(SessionKeys.UserEmail);
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    private string GetRequiredLoggedInUserEmail()
    {
        return GetLoggedInUserEmail()
            ?? throw new OrderPlacementException("Không xác định được tài khoản đặt hàng.");
    }

    private IActionResult RedirectToLogin(string mode)
    {
        return RedirectToAction(
            "Login",
            "Account",
            new
            {
                returnUrl = Url.Action("Index", "Checkout", new { mode })
            });
    }

    private static string BuildDeliveryAddress(CheckoutViewModel model)
    {
        var addressParts = new[]
        {
            model.AddressDetail,
            ResolveLocationName(model.WardName, model.Ward),
            ResolveLocationName(model.DistrictName, model.District),
            ResolveLocationName(model.ProvinceName, model.Province)
        };

        var address = string.Join(
            ", ",
            addressParts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim()));

        return string.IsNullOrWhiteSpace(address)
            ? "Địa chỉ sẽ được xác nhận qua điện thoại."
            : address;
    }

    private static string BuildShippingDetail(CheckoutViewModel model)
    {
        var parts = new[]
        {
            model.AddressDetail,
            ResolveLocationName(model.DistrictName, model.District)
        };

        return string.Join(
            ", ",
            parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim()));
    }

    private static string ResolveLocationName(string? displayName, string fallback) =>
        string.IsNullOrWhiteSpace(displayName)
            ? fallback.Trim()
            : displayName.Trim();

    private static string FormatItemCount(
        IReadOnlyCollection<CheckoutItemViewModel> items)
    {
        var totalQuantity = items.Sum(item => item.Quantity);
        return totalQuantity > 0
            ? $"{totalQuantity} sản phẩm"
            : "Đơn hàng đang cập nhật";
    }

    private static string FormatDiscount(decimal discount)
    {
        return discount > 0
            ? $"-{CheckoutViewModel.FormatPrice(discount)}"
            : CheckoutViewModel.FormatPrice(0);
    }

    private static string GetPaymentMethodName(
        long paymentMethodId,
        IReadOnlyCollection<CheckoutPaymentMethodViewModel> paymentMethods)
    {
        return paymentMethods
            .FirstOrDefault(method => method.Id == paymentMethodId)
            ?.Name
            ?? "Phương thức thanh toán";
    }

    private static string ResolvePaymentProvider(
        long paymentMethodId,
        IReadOnlyCollection<CheckoutPaymentMethodViewModel> paymentMethods)
    {
        var method = paymentMethods.FirstOrDefault(m => m.Id == paymentMethodId);
        return method?.Name.ToLowerInvariant() ?? "";
    }
}
