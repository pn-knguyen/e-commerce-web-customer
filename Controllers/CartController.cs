using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Constants;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.DTOs.Cart;
using e_commerce_web_customer.Infrastructure.MockData;
using e_commerce_web_customer.Interfaces;
using e_commerce_web_customer.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Controllers;

public sealed class CartController(
    CartSessionService cartSession,
    ICartItemValidator cartItemValidator,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(bool demo = false)
    {
        if (!UseMockData)
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Cart") });
            }

            return View(new CartIndexViewModel
            {
                UseDatabaseCart = true,
                Items = (await DatabaseCartService.GetAsync(userId.Value))
                    .Select(MapToCartItemViewModel)
                    .ToList()
            });
        }

        var model = demo
            ? new CartIndexViewModel
            {
                Items = MockCartData.GetCartDemoItems()
            }
            : new CartIndexViewModel
            {
                Items = BuildCartItems(cartSession.Load())
            };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromBody] CartSessionItem? item)
    {
        if (item is null)
        {
            return BadRequest(new { error = "Cart item is invalid." });
        }

        try
        {
            if (!UseMockData)
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng." });
                }

                if (!item.ProductVariantId.HasValue || item.ProductVariantId <= 0)
                {
                    return BadRequest(new { error = "Biến thể sản phẩm không hợp lệ." });
                }

                await DatabaseCartService.AddAsync(userId.Value, item.ProductVariantId.Value, item.Quantity);
                var databaseItems = await DatabaseCartService.GetAsync(userId.Value);

                return Ok(new { count = databaseItems.Sum(cartItem => cartItem.Quantity) });
            }

            var validatedItem = await cartItemValidator.ValidateAsync(item);
            var items = cartSession.AddOrUpdate(validatedItem);
            return Ok(new { count = CountQuantity(items) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow([FromBody] CartSessionItem? item)
    {
        if (item is null)
        {
            return BadRequest(new { error = "Cart item is invalid." });
        }

        try
        {
            if (!UseMockData)
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Bạn cần đăng nhập để mua sản phẩm." });
                }

                if (!item.ProductVariantId.HasValue || item.ProductVariantId <= 0)
                {
                    return BadRequest(new { error = "Biến thể sản phẩm không hợp lệ." });
                }

                await DatabaseCartService.AddAsync(userId.Value, item.ProductVariantId.Value, item.Quantity);
                var databaseItems = await DatabaseCartService.GetAsync(userId.Value);

                return Ok(new
                {
                    count = databaseItems.Sum(cartItem => cartItem.Quantity),
                    redirectUrl = Url.Action(nameof(Index), "Cart")
                });
            }

            var validatedItem = await cartItemValidator.ValidateAsync(item);
            cartSession.SaveBuyNow(validatedItem);

            var items = cartSession.Load(); // Main cart count doesn't change, we just need it for UI
            return Ok(new
            {
                count = CountQuantity(items),
                redirectUrl = Url.Action("Index", "Checkout", new { mode = "buynow" })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSession([FromBody] List<CartSessionItem>? items)
    {
        if (items is null)
        {
            return BadRequest(new { error = "Cart is empty." });
        }

        if (!UseMockData)
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "Bạn cần đăng nhập để thanh toán." });
            }

            var selectedCartItemIds = items
                .Select(item => long.TryParse(item.Id, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToHashSet();
            var selectedItems = (await DatabaseCartService.GetAsync(userId.Value))
                .Where(item => selectedCartItemIds.Contains(item.Id))
                .Select(item => new CartSessionItem
                {
                    ProductVariantId = item.ProductVariantId,
                    Id = item.ProductVariantId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Name = string.IsNullOrWhiteSpace(item.ColorName)
                        ? item.ProductName
                        : $"{item.ProductName} {item.ColorName}",
                    ProductUrl = $"/product/{item.ProductSlug}?variantId={item.ProductVariantId}",
                    ImageUrl = item.ImagePath ?? "/images/logo-techstore-icon.svg",
                    ImageAlt = item.ProductName,
                    Variant = item.ColorName ?? item.VariantCode,
                    UnitPrice = Math.Max(0, item.UnitPrice - item.DiscountValue),
                    Quantity = item.Quantity
                })
                .ToList();

            if (selectedItems.Count == 0)
            {
                return BadRequest(new { error = "Cart is empty." });
            }

            cartSession.Save(selectedItems);
            return Ok(new
            {
                saved = selectedItems.Count,
                count = selectedItems.Sum(item => item.Quantity)
            });
        }

        if (items.Count == 0)
        {
            cartSession.Clear();
            return Ok(new { saved = 0, count = 0 });
        }

        var validatedItems = new List<CartSessionItem>();
        foreach (var item in items)
        {
            try
            {
                validatedItems.Add(await cartItemValidator.ValidateAsync(item));
            }
            catch
            {
                // Skip invalid items
            }
        }

        cartSession.Save(validatedItems);
        var savedItems = cartSession.Load();

        if (savedItems.Count == 0)
        {
            return BadRequest(new { error = "Cart is empty or all items were invalid." });
        }

        return Ok(new
        {
            saved = savedItems.Count,
            count = CountQuantity(savedItems)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest? request)
    {
        if (UseMockData)
        {
            return BadRequest(new { error = "Database cart actions are unavailable in mock mode." });
        }

        if (request is null || request.CartItemId <= 0 || request.Quantity < 1)
        {
            return BadRequest(new { error = "Dữ liệu giỏ hàng không hợp lệ." });
        }

        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "Bạn cần đăng nhập để cập nhật giỏ hàng." });
        }

        try
        {
            var item = await DatabaseCartService.UpdateAsync(userId.Value, request.CartItemId, request.Quantity);
            if (item is null)
            {
                return NotFound(new { error = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            var items = await DatabaseCartService.GetAsync(userId.Value);
            return Ok(new { item, count = items.Sum(cartItem => cartItem.Quantity) });
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem([FromBody] DeleteCartItemRequest? request)
    {
        if (UseMockData)
        {
            return BadRequest(new { error = "Database cart actions are unavailable in mock mode." });
        }

        if (request is null || request.CartItemId <= 0)
        {
            return BadRequest(new { error = "Sản phẩm trong giỏ hàng không hợp lệ." });
        }

        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "Bạn cần đăng nhập để cập nhật giỏ hàng." });
        }

        if (!await DatabaseCartService.DeleteAsync(userId.Value, request.CartItemId))
        {
            return NotFound(new { error = "Không tìm thấy sản phẩm trong giỏ hàng." });
        }

        var items = await DatabaseCartService.GetAsync(userId.Value);
        return Ok(new { count = items.Sum(cartItem => cartItem.Quantity) });
    }

    private IReadOnlyList<CartItemViewModel> BuildCartItems(IEnumerable<CartSessionItem> items)
    {
        return items.Select(item => new CartItemViewModel
        {
            Id = item.Id,
            Name = item.Name,
            ProductUrl = ResolveProductUrl(item),
            ImageUrl = item.ImageUrl,
            ImageAlt = item.ImageAlt,
            Variant = item.Variant,
            UnitPrice = item.UnitPrice,
            Quantity = Math.Max(1, item.Quantity)
        }).ToList();
    }

    private string ResolveProductUrl(CartSessionItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.ProductUrl))
        {
            return item.ProductUrl;
        }

        return Url.Action("Details", "Product", new { slug = item.Id }) ?? $"/product/{item.Id}";
    }

    private static int CountQuantity(IEnumerable<CartSessionItem> items)
    {
        return items.Sum(item => Math.Max(1, item.Quantity));
    }

    private bool UseMockData => configuration.GetValue<bool>("DatabaseSettings:UseMockData", true);
    private ICartService DatabaseCartService => serviceProvider.GetRequiredService<ICartService>();
    private EcommerceDbContext DatabaseContext => serviceProvider.GetRequiredService<EcommerceDbContext>();

    private async Task<long?> GetCurrentUserIdAsync()
    {
        var sessionUserId = HttpContext.Session.GetString(SessionKeys.UserId);
        if (long.TryParse(sessionUserId, out var userId) && userId > 0)
        {
            return userId;
        }

        var email = HttpContext.Session.GetString(SessionKeys.UserEmail);
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var resolvedUserId = await DatabaseContext.Users
            .Where(user => user.IsActive && user.Email == email)
            .Select(user => (long?)user.Id)
            .FirstOrDefaultAsync();

        if (resolvedUserId.HasValue)
        {
            HttpContext.Session.SetString(SessionKeys.UserId, resolvedUserId.Value.ToString());
        }

        return resolvedUserId;
    }

    private static CartItemViewModel MapToCartItemViewModel(CartItemDto item)
    {
        return new CartItemViewModel
        {
            Id = item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Name = string.IsNullOrWhiteSpace(item.ColorName)
                ? item.ProductName
                : $"{item.ProductName} {item.ColorName}",
            ProductUrl = $"/product/{item.ProductSlug}?variantId={item.ProductVariantId}",
            ImageUrl = item.ImagePath ?? "/images/logo-techstore-icon.svg",
            ImageAlt = item.ProductName,
            Variant = item.ColorName ?? item.VariantCode,
            UnitPrice = Math.Max(0, item.UnitPrice - item.DiscountValue),
            Quantity = item.Quantity,
            MaxQuantity = item.AvailableQuantity > 0 ? item.AvailableQuantity : 99
        };
    }
}

public sealed class UpdateCartItemRequest
{
    public long CartItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class DeleteCartItemRequest
{
    public long CartItemId { get; set; }
}
