using Microsoft.AspNetCore.Http;

namespace e_commerce_web_customer.Application.Contracts;

public interface IVnPayIntegration
{
    /// <summary>
    /// Calls VNPay API to create a payment URL and returns it.
    /// </summary>
    Task<VnPayCreateResult> CreatePaymentUrlAsync(
        VnPayCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the HMAC-SHA512 signature from VNPay callback.
    /// </summary>
    VnPayCallbackResult ProcessCallback(IQueryCollection query);
}

public sealed record VnPayCreateRequest(
    string OrderId,
    long Amount,
    string OrderInfo,
    string ReturnUrl,
    string IpnUrl,
    string IpAddress);

public sealed record VnPayCreateResult(
    bool Success,
    string? PayUrl,
    string? ErrorMessage = null);

public sealed record VnPayCallbackResult(
    bool Success,
    string OrderId,
    string TransactionId,
    string ResultCode,
    string Message,
    bool IsValidSignature);
