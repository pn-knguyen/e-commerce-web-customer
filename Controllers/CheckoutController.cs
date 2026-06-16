using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.DTOs.Order;
using e_commerce_web_customer.Infrastructure.Cart.Mock;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Cart;
using e_commerce_web_customer.ViewModels.Checkout;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace e_commerce_web_customer.Controllers;

public sealed class CheckoutController(
    CartSessionService cartSession,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : Controller
{
    private const string SuccessSessionKey = "checkout_success_order";

    [HttpGet]
    public IActionResult Index(bool demo = false, string mode = "")
    {
        if (HttpContext.Session.GetString(e_commerce_web_customer.Application.Constants.SessionKeys.IsLoggedIn) != "true")
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout", new { mode = mode }) });
        }

        var model = BuildModel(demo, mode);

        if (model.Items.Count == 0 && !demo)
        {
            return RedirectToAction("Index", "Cart");
        }

        ViewData["Mode"] = mode;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model, [FromQuery] string mode = "")
    {
        if (HttpContext.Session.GetString(e_commerce_web_customer.Application.Constants.SessionKeys.IsLoggedIn) != "true")
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout", new { mode = mode }) });
        }

        if (!ModelState.IsValid)
        {
            // Re-populate read-only order summary before returning view
            var fresh = BuildModel(mode: mode);
            model.Items       = fresh.Items;
            model.ShippingFee = fresh.ShippingFee;
            model.Discount    = fresh.Discount;
            ViewData["Mode"] = mode;
            return View(model);
        }

        var orderSnapshot = BuildModel(mode: mode);
        if (orderSnapshot.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var sessionItems = mode == "buynow" ? cartSession.LoadBuyNow() : cartSession.Load();

        if (UseMockData)
        {
            StoreSuccessModel(BuildMockSuccessModel(model, orderSnapshot));
            ClearSubmittedCart(mode);
            return RedirectToAction(nameof(Success));
        }

        var userIdValue = HttpContext.Session.GetString(SessionKeys.UserId);
        if (!long.TryParse(userIdValue, out var userId) || userId <= 0)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action("Index", "Checkout", new { mode })
            });
        }

        OrderDto order;
        try
        {
            order = await DatabaseOrderService.CreateAsync(new CreateOrderRequest
            {
                UserId = userId,
                ContactName = model.FullName,
                Phone = model.Phone,
                Province = model.Province,
                Ward = model.Ward,
                DetailAddress = string.Join(
                    ", ",
                    new[] { model.AddressDetail, model.District }
                        .Where(value => !string.IsNullOrWhiteSpace(value))),
                PaymentMethod = model.PaymentMethod.ToString(),
                ShippingFee = orderSnapshot.ShippingFee,
                Items = sessionItems
                    .Where(item => item.ProductVariantId.HasValue)
                    .Select(item => new CreateOrderItemRequest
                    {
                        ProductVariantId = item.ProductVariantId!.Value,
                        Quantity = item.Quantity
                    })
                    .ToList()
            });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Items = orderSnapshot.Items;
            model.ShippingFee = orderSnapshot.ShippingFee;
            model.Discount = orderSnapshot.Discount;
            ViewData["Mode"] = mode;
            return View(model);
        }

        StoreSuccessModel(BuildSuccessModel(model, order));
        ClearSubmittedCart(mode);
        
        return RedirectToAction(nameof(Success));
    }

    [HttpGet]
    public IActionResult Success()
    {
        var successModel = ReadSuccessModel();
        if (successModel is null)
        {
            return RedirectToAction("Index", "Cart");
        }

        return View(successModel);
    }

    // Build model from session. Demo data is opt-in through ?demo=true.
    private CheckoutViewModel BuildModel(bool demo = false, string mode = "")
    {
        var sessionItems = mode == "buynow" ? cartSession.LoadBuyNow() : cartSession.Load();

        var items = sessionItems.Count > 0
            ? sessionItems.Select(i => new CheckoutItemViewModel
            {
                Name      = i.Name,
                ImageUrl  = i.ImageUrl,
                ImageAlt  = i.ImageAlt,
                Variant   = i.Variant,
                UnitPrice = i.UnitPrice,
                Quantity  = i.Quantity,
            }).ToList()
            : demo ? MockCartData.GetCheckoutDemoItems() : [];

        return new CheckoutViewModel
        {
            Items       = items,
            ShippingFee = 30_000m,
            Discount    = 0m,
        };
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

    private void StoreSuccessModel(CheckoutSuccessViewModel model)
    {
        HttpContext.Session.SetString(SuccessSessionKey, JsonSerializer.Serialize(model));
    }

    private void ClearSubmittedCart(string mode)
    {
        if (mode == "buynow")
        {
            cartSession.ClearBuyNow();
            return;
        }

        cartSession.Clear();
    }

    private static CheckoutSuccessViewModel BuildMockSuccessModel(
        CheckoutViewModel submittedModel,
        CheckoutViewModel order)
    {
        var placedAt = DateTime.Now;

        return new CheckoutSuccessViewModel
        {
            OrderCode = $"TS{placedAt:yyMMddHHmmss}",
            CustomerName = string.IsNullOrWhiteSpace(submittedModel.FullName)
                ? "Quý khách"
                : submittedModel.FullName.Trim(),
            Phone = submittedModel.Phone?.Trim() ?? string.Empty,
            Email = submittedModel.Email?.Trim() ?? string.Empty,
            DeliveryAddress = BuildDeliveryAddress(submittedModel),
            ShippingMethodName = "Giao hàng nhanh",
            PaymentMethodName = GetPaymentMethodName(submittedModel.PaymentMethod),
            PlacedAt = placedAt.ToString("HH:mm, dd/MM/yyyy"),
            EstimatedDeliveryDateText = placedAt.AddDays(2).ToString("dd/MM/yyyy"),
            ItemCountText = $"{order.Items.Sum(item => item.Quantity)} sản phẩm",
            SubtotalText = CheckoutViewModel.FormatPrice(order.Subtotal),
            ShippingFeeText = CheckoutViewModel.FormatPrice(order.ShippingFee),
            DiscountText = FormatDiscount(order.Discount),
            TotalText = CheckoutViewModel.FormatPrice(order.Total),
            Items = order.Items.Select(item => new CheckoutSuccessItemViewModel
            {
                Name = item.Name,
                ImageUrl = item.ImageUrl,
                ImageAlt = item.ImageAlt,
                Variant = item.Variant,
                Quantity = item.Quantity,
                LineTotalText = CheckoutViewModel.FormatPrice(item.UnitPrice * item.Quantity)
            }).ToList()
        };
    }

    private static CheckoutSuccessViewModel BuildSuccessModel(
        CheckoutViewModel submittedModel,
        OrderDto order)
        => new()
        {
            OrderCode = order.OrderCode,
            CustomerName = string.IsNullOrWhiteSpace(submittedModel.FullName) ? "Quý khách" : submittedModel.FullName.Trim(),
            Phone = submittedModel.Phone?.Trim() ?? string.Empty,
            Email = submittedModel.Email?.Trim() ?? string.Empty,
            DeliveryAddress = BuildDeliveryAddress(submittedModel),
            ShippingMethodName = "Giao hàng nhanh",
            PaymentMethodName = order.PaymentMethodName,
            PlacedAt = order.CreatedAt.ToLocalTime().ToString("HH:mm, dd/MM/yyyy"),
            EstimatedDeliveryDateText = order.CreatedAt.ToLocalTime().AddDays(2).ToString("dd/MM/yyyy"),
            ItemCountText = FormatItemCount(order.Items),
            SubtotalText = CheckoutViewModel.FormatPrice(order.SubtotalAmount),
            ShippingFeeText = CheckoutViewModel.FormatPrice(order.ShippingFee),
            DiscountText = FormatDiscount(order.VoucherDiscount),
            TotalText = CheckoutViewModel.FormatPrice(order.TotalAmount),
            Items = BuildSuccessItems(order.Items),
        };

    private static List<CheckoutSuccessItemViewModel> BuildSuccessItems(IEnumerable<OrderItemDto> items)
        => items.Select(item => new CheckoutSuccessItemViewModel
        {
            Name = string.IsNullOrWhiteSpace(item.VariantName)
                ? item.ProductName
                : $"{item.ProductName} {item.VariantName}",
            ImageUrl = item.ImagePath ?? "/images/logo-techstore-icon.svg",
            ImageAlt = item.ProductName,
            Variant = item.VariantName,
            Quantity = item.Quantity,
            LineTotalText = CheckoutViewModel.FormatPrice(item.UnitPrice * item.Quantity),
        }).ToList();

    private static string BuildDeliveryAddress(CheckoutViewModel model)
    {
        var addressParts = new[]
        {
            model.AddressDetail,
            model.Ward,
            model.District,
            model.Province,
        };

        var address = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()));
        return string.IsNullOrWhiteSpace(address) ? "Địa chỉ sẽ được xác nhận qua điện thoại." : address;
    }

    private static string FormatItemCount(IReadOnlyCollection<OrderItemDto> items)
    {
        var totalQuantity = items.Sum(item => item.Quantity);
        return totalQuantity > 0 ? $"{totalQuantity} sản phẩm" : "Đơn hàng đang cập nhật";
    }

    private static string FormatDiscount(decimal discount)
        => discount > 0 ? $"-{CheckoutViewModel.FormatPrice(discount)}" : CheckoutViewModel.FormatPrice(0);

    private static string GetPaymentMethodName(PaymentMethod paymentMethod) => paymentMethod switch
    {
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        PaymentMethod.Momo => "Ví MoMo",
        PaymentMethod.VnPay => "VNPay",
        PaymentMethod.ZaloPay => "ZaloPay",
        _ => "Thanh toán khi nhận hàng"
    };

    private bool UseMockData => configuration.GetValue<bool>("DatabaseSettings:UseMockData", true);
    private IOrderService DatabaseOrderService => serviceProvider.GetRequiredService<IOrderService>();
}
