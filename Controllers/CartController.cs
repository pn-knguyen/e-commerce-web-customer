using e_commerce_web_customer.Application.Contracts;
using e_commerce_web_customer.Application.Services;
using e_commerce_web_customer.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

public sealed class CartController(
    CartSessionService cartSession,
    ICartItemValidator cartItemValidator,
    ICartDemoDataProvider demoDataProvider) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        bool demo = false,
        CancellationToken cancellationToken = default)
    {
        var items = demo
            ? await demoDataProvider.GetCartItemsAsync(cancellationToken)
            : BuildCartItems(cartSession.Load());

        return View(new CartIndexViewModel
        {
            Items = items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(
        [FromBody] CartSessionItem? item,
        CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return BadRequest(new { error = "Cart item is invalid." });
        }

        try
        {
            var validatedItem = await cartItemValidator.ValidateAsync(
                item,
                cancellationToken);
            var items = cartSession.AddOrUpdate(validatedItem);
            return Ok(new { count = CountQuantity(items) });
        }
        catch (CartItemValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(
        [FromBody] CartSessionItem? item,
        CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return BadRequest(new { error = "Cart item is invalid." });
        }

        try
        {
            var validatedItem = await cartItemValidator.ValidateAsync(
                item,
                cancellationToken);
            cartSession.SaveBuyNow(validatedItem);

            var items = cartSession.Load(); // Main cart count doesn't change, we just need it for UI
            return Ok(new
            {
                count = CountQuantity(items),
                redirectUrl = Url.Action("Index", "Checkout", new { mode = "buynow" })
            });
        }
        catch (CartItemValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSession(
        [FromBody] List<CartSessionItem>? items,
        CancellationToken cancellationToken)
    {
        if (items is null)
        {
            return BadRequest(new { error = "Cart is empty." });
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
                validatedItems.Add(await cartItemValidator.ValidateAsync(
                    item,
                    cancellationToken));
            }
            catch (CartItemValidationException)
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
}
