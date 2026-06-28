using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using e_commerce_web_customer.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace e_commerce_web_customer.Infrastructure.Integrations.VnPay;

public sealed class VnPayIntegration : IVnPayIntegration
{
    private readonly VnPayOptions _opts;

    public VnPayIntegration(IOptions<VnPayOptions> options)
    {
        _opts = options.Value;
    }

    public Task<VnPayCreateResult> CreatePaymentUrlAsync(
        VnPayCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        // Amount needs to be multiplied by 100 per VNPay docs
        // Must be formatted as an integer string without decimals to prevent signature mismatch
        var amount = (request.Amount * 100).ToString("0", CultureInfo.InvariantCulture);
        
        // Đảm bảo luôn lấy giờ Việt Nam (UTC+7) trên mọi server (để tránh lỗi quá hạn trên Linux/Railway)
        var vnTime = DateTime.UtcNow.AddHours(7);
        var createDate = vnTime.ToString("yyyyMMddHHmmss");
        var expireDate = vnTime.AddMinutes(15).ToString("yyyyMMddHHmmss");

        var ipAddr = request.IpAddress;
        if (string.IsNullOrEmpty(ipAddr) || ipAddr == "::1")
        {
            ipAddr = "127.0.0.1";
        }

        var vnp_Params = new SortedList<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", _opts.TmnCode },
            { "vnp_Amount", amount },
            { "vnp_CreateDate", createDate },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", ipAddr },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", request.OrderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", request.ReturnUrl },
            { "vnp_TxnRef", request.OrderId },
            { "vnp_ExpireDate", expireDate }
        };

        var queryString = BuildQueryString(vnp_Params);
        var signData = queryString;
        var vnp_SecureHash = HmacSha512(_opts.HashSecret, signData);

        var paymentUrl = $"{_opts.BaseUrl}?{queryString}&vnp_SecureHash={vnp_SecureHash}";

        return Task.FromResult(new VnPayCreateResult(true, paymentUrl));
    }

    public VnPayCallbackResult ProcessCallback(IQueryCollection query)
    {
        var vnp_Params = new SortedList<string, string>(StringComparer.Ordinal);
        string vnp_SecureHash = string.Empty;

        foreach (var kvp in query)
        {
            if (kvp.Key.StartsWith("vnp_"))
            {
                if (kvp.Key == "vnp_SecureHashType") continue;
                if (kvp.Key == "vnp_SecureHash")
                {
                    vnp_SecureHash = kvp.Value.ToString();
                    continue;
                }

                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    vnp_Params.Add(kvp.Key, kvp.Value.ToString());
                }
            }
        }

        var orderId = GetValue(query, "vnp_TxnRef");
        var transactionId = GetValue(query, "vnp_TransactionNo");
        var responseCode = GetValue(query, "vnp_ResponseCode");

        var signData = BuildQueryString(vnp_Params);
        var computedHash = HmacSha512(_opts.HashSecret, signData);

        var signatureValid = string.Equals(computedHash, vnp_SecureHash, StringComparison.OrdinalIgnoreCase);

        if (!signatureValid)
        {
            return new VnPayCallbackResult(false, orderId, transactionId, responseCode, "Chữ ký VNPay không hợp lệ.", false);
        }

        var success = responseCode == "00";
        var message = success
            ? "Thanh toán VNPay thành công."
            : $"Giao dịch VNPay thất bại (mã {responseCode}).";

        return new VnPayCallbackResult(success, orderId, transactionId, responseCode, message, true);
    }

    private static string GetValue(IQueryCollection query, string key) =>
        query.TryGetValue(key, out var v) ? v.ToString() : string.Empty;

    private static string BuildQueryString(SortedList<string, string> requestData)
    {
        var data = new StringBuilder();
        foreach (var kv in requestData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }
        var queryString = data.ToString();
        if (queryString.Length > 0)
        {
            queryString = queryString.Remove(data.Length - 1, 1);
        }
        return queryString;
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}
