using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using CatalogProduct = e_commerce_web_customer.Models.Entities.Product;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Rules;

public sealed class IphoneAccessoryCompatibilityRule : ICompatibilityRule
{
    private static readonly IReadOnlyCollection<string> AppleBrandSlugs = ["apple"];
    private static readonly IReadOnlyCollection<string> AppleAccessoryCategorySlugs = ["phu-kien-apple"];

    public string Name => "iphone-accessory-compatibility";
    public IReadOnlyCollection<string> CandidateBrandSlugs => AppleBrandSlugs;
    public IReadOnlyCollection<string> CandidateCategorySlugs => AppleAccessoryCategorySlugs;

    public bool CanHandle(ProductRecommendationRequest request)
    {
        return ResolveIphoneModelKey($"{request.ProductName} {request.ProductSlug}") is not null;
    }

    public RecommendationCandidate? Evaluate(
        CatalogProduct accessory,
        ProductRecommendationRequest request)
    {
        var targetModel = ResolveIphoneModelKey($"{request.ProductName} {request.ProductSlug}");
        if (targetModel is null)
        {
            return null;
        }

        var variant = accessory.ProductVariants
            .Where(item => item.IsActive && item.Quantity > 0)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.Id)
            .FirstOrDefault();
        if (variant is null)
        {
            return null;
        }

        var searchableText = RecommendationTextNormalizer.Normalize($"{accessory.Name} {accessory.Slug}");
        var modelLabel = BuildIphoneModelLabel(targetModel);

        if (searchableText.Contains("op lung", StringComparison.Ordinal))
        {
            var accessoryModel = ResolveIphoneModelKey(searchableText);
            if (!string.Equals(accessoryModel, targetModel, StringComparison.Ordinal))
            {
                return null;
            }

            var score = 1000;
            if (searchableText.Contains("silicone", StringComparison.Ordinal))
            {
                score += 30;
            }
            else if (searchableText.Contains("clear", StringComparison.Ordinal)
                || searchableText.Contains("trong suot", StringComparison.Ordinal))
            {
                score += 20;
            }
            else if (searchableText.Contains("techwoven", StringComparison.Ordinal))
            {
                score += 10;
            }

            return new RecommendationCandidate(
                accessory,
                variant,
                $"Đúng mẫu {modelLabel}, hỗ trợ MagSafe",
                score,
                Name);
        }

        if (searchableText.Contains("applecare", StringComparison.Ordinal))
        {
            var accessoryModel = ResolveIphoneModelKey(searchableText);
            return string.Equals(accessoryModel, targetModel, StringComparison.Ordinal)
                ? new RecommendationCandidate(
                    accessory,
                    variant,
                    $"Bảo vệ chính hãng cho {modelLabel}",
                    850,
                    Name)
                : null;
        }

        if (string.Equals(targetModel, "iphone air", StringComparison.Ordinal)
            && searchableText.Contains("pin", StringComparison.Ordinal)
            && searchableText.Contains("iphone air", StringComparison.Ordinal)
            && searchableText.Contains("magsafe", StringComparison.Ordinal))
        {
            return new RecommendationCandidate(
                accessory,
                variant,
                "Pin MagSafe thiết kế riêng cho iPhone Air",
                820,
                Name);
        }

        if (IsUsbCiphoneModel(targetModel))
        {
            if (IsAppleUsbCCharger(searchableText))
            {
                return new RecommendationCandidate(
                    accessory,
                    variant,
                    "Sạc nhanh USB-C phù hợp iPhone đời mới",
                    700,
                    Name);
            }

            if (IsAppleUsbCToUsbCCable(searchableText))
            {
                return new RecommendationCandidate(
                    accessory,
                    variant,
                    "Cáp USB-C phù hợp iPhone đời mới",
                    680,
                    Name);
            }

            if (searchableText.Contains("earpods", StringComparison.Ordinal)
                && searchableText.Contains("usb c", StringComparison.Ordinal))
            {
                return new RecommendationCandidate(
                    accessory,
                    variant,
                    "Tai nghe USB-C dùng trực tiếp với iPhone",
                    560,
                    Name);
            }
        }
        else
        {
            if (IsAppleLightningCable(searchableText))
            {
                return new RecommendationCandidate(
                    accessory,
                    variant,
                    "Cáp Lightning phù hợp iPhone đời trước",
                    680,
                    Name);
            }

            if (searchableText.Contains("earpods", StringComparison.Ordinal)
                && searchableText.Contains("lightning", StringComparison.Ordinal))
            {
                return new RecommendationCandidate(
                    accessory,
                    variant,
                    "Tai nghe Lightning dùng trực tiếp với iPhone",
                    560,
                    Name);
            }
        }

        if (searchableText.Contains("airpods 4", StringComparison.Ordinal)
            || searchableText.Contains("airpods pro", StringComparison.Ordinal))
        {
            return new RecommendationCandidate(
                accessory,
                variant,
                "Kết nối nhanh với hệ sinh thái Apple",
                520,
                Name);
        }

        return null;
    }

    private static string? ResolveIphoneModelKey(string value)
    {
        var normalized = RecommendationTextNormalizer.Normalize(value);
        string[] knownModels =
        [
            "iphone 17 pro max",
            "iphone 17 pro",
            "iphone 17e",
            "iphone 17",
            "iphone air",
            "iphone 16 pro max",
            "iphone 16 pro",
            "iphone 16 plus",
            "iphone 16e",
            "iphone 16",
            "iphone 15 pro max",
            "iphone 15 pro",
            "iphone 15 plus",
            "iphone 15",
            "iphone 14 pro max",
            "iphone 14 pro",
            "iphone 14 plus",
            "iphone 14",
            "iphone 13 pro max",
            "iphone 13 mini",
            "iphone 13",
            "iphone 12 pro max"
        ];

        return knownModels.FirstOrDefault(model =>
            normalized.Contains(model, StringComparison.Ordinal));
    }

    private static string BuildIphoneModelLabel(string modelKey)
    {
        if (string.Equals(modelKey, "iphone air", StringComparison.Ordinal))
        {
            return "iPhone Air";
        }

        var parts = modelKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "iPhone";
        }

        return "iPhone " + string.Join(' ', parts.Skip(1).Select(part =>
            part switch
            {
                "pro" => "Pro",
                "max" => "Max",
                "plus" => "Plus",
                "mini" => "mini",
                _ => part
            }));
    }

    private static bool IsUsbCiphoneModel(string modelKey)
    {
        var normalized = RecommendationTextNormalizer.Normalize(modelKey);
        return normalized.Contains("iphone air", StringComparison.Ordinal)
            || normalized.Contains("iphone 17", StringComparison.Ordinal)
            || normalized.Contains("iphone 16", StringComparison.Ordinal)
            || normalized.Contains("iphone 15", StringComparison.Ordinal);
    }

    private static bool IsAppleUsbCCharger(string text)
    {
        return (text.Contains("cu sac", StringComparison.Ordinal)
                || text.Contains("sac nhanh", StringComparison.Ordinal)
                || text.Contains("charger", StringComparison.Ordinal)
                || text.Contains("adapter", StringComparison.Ordinal))
            && text.Contains("20w", StringComparison.Ordinal)
            && (text.Contains("usb c", StringComparison.Ordinal)
                || text.Contains("type c", StringComparison.Ordinal));
    }

    private static bool IsAppleUsbCToUsbCCable(string text)
    {
        if (!text.Contains("cap", StringComparison.Ordinal)
            || text.Contains("lightning", StringComparison.Ordinal))
        {
            return false;
        }

        return text.Contains("usb c to type c", StringComparison.Ordinal)
            || text.Contains("type c to type c", StringComparison.Ordinal)
            || text.Contains("usb c to usb c", StringComparison.Ordinal);
    }

    private static bool IsAppleLightningCable(string text)
    {
        return text.Contains("cap", StringComparison.Ordinal)
            && text.Contains("lightning", StringComparison.Ordinal);
    }
}
