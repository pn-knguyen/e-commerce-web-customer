using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

[Route("wishlist")]
public sealed class WishlistController(IWishlistService wishlistService) : Controller
{
    [HttpPost("toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        [FromBody] WishlistToggleRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return Unauthorized(new
            {
                loginUrl = Url.Action("Login", "Account", new { returnUrl = Request.Headers.Referer.ToString() })
            });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.ProductId))
        {
            return BadRequest(new { message = "Sản phẩm không hợp lệ." });
        }

        var result = await wishlistService.ToggleAsync(
            GetRequiredLoggedInUserEmail(),
            request.ProductId,
            cancellationToken);

        return result.Success
            ? Ok(new { isWishlisted = result.IsWishlisted, message = result.Message })
            : BadRequest(new { message = result.Message });
    }

    [HttpPost("remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        [FromBody] WishlistToggleRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return Unauthorized(new
            {
                loginUrl = Url.Action("Login", "Account", new { returnUrl = Request.Headers.Referer.ToString() })
            });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.ProductId))
        {
            return BadRequest(new { message = "Sản phẩm không hợp lệ." });
        }

        var result = await wishlistService.RemoveAsync(
            GetRequiredLoggedInUserEmail(),
            request.ProductId,
            cancellationToken);

        return result.Success
            ? Ok(new { message = result.Message })
            : BadRequest(new { message = result.Message });
    }

    [HttpPost("status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(
        [FromBody] WishlistStatusRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn())
        {
            return Unauthorized(new
            {
                loginUrl = Url.Action("Login", "Account", new { returnUrl = Request.Headers.Referer.ToString() })
            });
        }

        var productIds = request?.ProductIds?
            .Where(productId => !string.IsNullOrWhiteSpace(productId))
            .Select(productId => productId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList() ?? [];
        if (productIds.Count == 0)
        {
            return Ok(new { statuses = new Dictionary<string, bool>() });
        }

        var statuses = await wishlistService.GetStatusesAsync(
            GetRequiredLoggedInUserEmail(),
            productIds,
            cancellationToken);

        return Ok(new { statuses });
    }

    private bool IsLoggedIn()
    {
        return HttpContext.Session.GetString(SessionKeys.IsLoggedIn) == "true";
    }

    private string GetRequiredLoggedInUserEmail()
    {
        return HttpContext.Session.GetString(SessionKeys.UserEmail)?.Trim()
            ?? string.Empty;
    }
}

public sealed record WishlistToggleRequest(string ProductId);

public sealed record WishlistStatusRequest(IReadOnlyList<string> ProductIds);
