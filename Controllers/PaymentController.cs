using e_commerce_web_customer.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

/// <summary>
/// Handles MoMo payment callbacks — mirrors payment_result_screen.dart in sportset_customer.
/// Flutter: deeplink yourapp://payment-result?resultCode=...
/// Web:     GET /payment/momo-return?resultCode=...&orderId=...&transId=...
/// </summary>
public sealed class PaymentController(
    IMoMoIntegration momoIntegration,
    IVnPayIntegration vnPayIntegration,
    IOrderService orderService,
    e_commerce_web_customer.Application.Services.CartSessionService cartSession,
    ILogger<PaymentController> logger) : Controller
{
    /// <summary>
    /// MoMo redirects here after payment (equivalent to Flutter deeplink handler).
    /// MoMo passes: resultCode, orderId, requestId, amount, orderInfo,
    ///              orderType, transId, message, payType, responseTime,
    ///              extraData, signature
    /// </summary>
    [HttpGet("payment/momo-return")]
    public async Task<IActionResult> MoMoReturn(CancellationToken cancellationToken)
    {
        // Log full callback URL for debugging signature issues
        var qs = string.Join(" | ", Request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
        logger.LogInformation("[MoMo Callback] {QueryString}", qs);

        var result = momoIntegration.ProcessCallback(Request.Query);

        if (!string.IsNullOrEmpty(result.OrderId))
        {
            await HandlePaymentResultAsync(result.Success, result.OrderId, true, cancellationToken);
        }

        return View("MoMoResult", new MoMoResultViewModel(
            Success: result.Success,
            OrderId: result.OrderId,
            TransactionId: result.TransactionId,
            ResultCode: result.ResultCode,
            Message: result.Message));
    }

    /// <summary>
    /// IPN (server-to-server notification) from MoMo.
    /// MoMo calls this in the background independently of the redirect.
    /// </summary>
    [HttpPost("payment/momo-ipn")]
    public async Task<IActionResult> MoMoIpn([FromBody] MoMoIpnRequest request, CancellationToken cancellationToken)
    {
        var result = momoIntegration.ProcessIpn(request);

        if (!string.IsNullOrEmpty(result.OrderId))
        {
            await HandlePaymentResultAsync(result.Success, result.OrderId, false, cancellationToken);
        }

        // MoMo expects HTTP 204 or a JSON ack for IPN
        return Ok(new { statusCode = 0, message = "Received" });
    }

    /// <summary>
    /// VNPay redirects here after payment.
    /// </summary>
    [HttpGet("payment/vnpay-return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var qs = string.Join(" | ", Request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
        logger.LogInformation("[VNPay Callback] {QueryString}", qs);

        var result = vnPayIntegration.ProcessCallback(Request.Query);

        if (!string.IsNullOrEmpty(result.OrderId))
        {
            await HandlePaymentResultAsync(result.Success, result.OrderId, true, cancellationToken);
        }

        return View("VnPayResult", new VnPayResultViewModel(
            Success: result.Success,
            OrderId: result.OrderId,
            TransactionId: result.TransactionId,
            ResultCode: result.ResultCode,
            Message: result.Message));
    }

    /// <summary>
    /// IPN from VNPay.
    /// </summary>
    [HttpGet("payment/vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var result = vnPayIntegration.ProcessCallback(Request.Query);

        if (!result.IsValidSignature)
        {
            return Ok(new { RspCode = "97", Message = "Invalid Checksum" });
        }

        if (!string.IsNullOrEmpty(result.OrderId))
        {
            await HandlePaymentResultAsync(result.Success, result.OrderId, false, cancellationToken);
        }

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    private async Task HandlePaymentResultAsync(bool success, string orderId, bool clearSession, CancellationToken cancellationToken)
    {
        if (success)
        {
            await orderService.ConfirmOnlinePaymentAsync(orderId, cancellationToken);
            if (clearSession)
            {
                cartSession.Clear();
                cartSession.ClearBuyNow();
            }
        }
        else
        {
            await orderService.CancelFailedOrderAsync(orderId, cancellationToken);
        }
    }
}

public sealed record VnPayResultViewModel(
    bool Success,
    string OrderId,
    string TransactionId,
    string ResultCode,
    string Message);

public sealed record MoMoResultViewModel(
    bool Success,
    string OrderId,
    string TransactionId,
    string ResultCode,
    string Message);
