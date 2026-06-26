using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers.Payments;

public sealed class SePayPaymentController(
    ISePayPaymentService sePayPaymentService,
    IOrderService orderService) : Controller
{
    [HttpGet("payment/sepay/{orderCode}")]
    public async Task<IActionResult> Index(
        string orderCode,
        CancellationToken cancellationToken)
    {
        var userEmail = GetLoggedInUserEmail();
        if (userEmail is null)
        {
            return RedirectToAction(
                "Login",
                "Account",
                new { returnUrl = Request.Path.Value });
        }

        var payment = await sePayPaymentService.GetPaymentAsync(
            orderCode,
            userEmail,
            cancellationToken);

        return payment is null ? NotFound() : View("~/Views/Payment/SePay.cshtml", payment);
    }

    [HttpGet("api/payments/sepay/{orderCode}/status")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Status(
        string orderCode,
        CancellationToken cancellationToken)
    {
        var userEmail = GetLoggedInUserEmail();
        if (userEmail is null)
        {
            return Unauthorized(new { status = "unauthorized" });
        }

        var result = await sePayPaymentService.GetStatusAsync(
            orderCode,
            userEmail,
            cancellationToken);

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";

        return result is null
            ? NotFound(new { status = "not_found" })
            : Ok(new { status = result.Status });
    }

    [HttpPost("payment/sepay/cancel")]
    public async Task<IActionResult> Cancel(
        [FromForm] string orderCode,
        CancellationToken cancellationToken)
    {
        var userEmail = GetLoggedInUserEmail();
        if (userEmail is null)
        {
            return RedirectToAction("Login", "Account");
        }

        await orderService.CancelFailedOrderAsync(orderCode, cancellationToken);
        return RedirectToAction("Index", "Cart");
    }

    private string? GetLoggedInUserEmail()
    {
        if (HttpContext.Session.GetString(SessionKeys.IsLoggedIn) != "true")
        {
            return null;
        }

        var email = HttpContext.Session.GetString(SessionKeys.UserEmail);
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
