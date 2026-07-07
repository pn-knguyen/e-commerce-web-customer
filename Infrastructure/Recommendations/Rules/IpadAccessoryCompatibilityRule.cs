using System.Text.RegularExpressions;
using e_commerce_web_customer.Application.Recommendations.Abstractions;
using e_commerce_web_customer.Application.Recommendations.Models;
using CatalogProduct = e_commerce_web_customer.Models.Entities.Product;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Rules;

public sealed class IpadAccessoryCompatibilityRule : ICompatibilityRule
{
    private static readonly IReadOnlyCollection<string> AppleBrandSlugs = ["apple"];
    private static readonly IReadOnlyCollection<string> AppleAccessoryCategorySlugs =
        ["phu-kien-apple", "apple-care"];

    public string Name => "ipad-accessory-compatibility";
    public IReadOnlyCollection<string> CandidateBrandSlugs => AppleBrandSlugs;
    public IReadOnlyCollection<string> CandidateCategorySlugs => AppleAccessoryCategorySlugs;

    public bool CanHandle(ProductRecommendationRequest request)
    {
        return ResolveIpadProfile($"{request.ProductName} {request.ProductSlug}") is not null;
    }

    public RecommendationCandidate? Evaluate(
        CatalogProduct accessory,
        ProductRecommendationRequest request)
    {
        var profile = ResolveIpadProfile($"{request.ProductName} {request.ProductSlug}");
        if (profile is null)
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

        var accessoryText = RecommendationTextNormalizer.Normalize($"{accessory.Name} {accessory.Slug}");

        if (IsAppleCare(accessoryText))
        {
            return IsIpadModelMatch(accessoryText, profile)
                ? Candidate(
                    accessory,
                    variant,
                    "Gói bảo vệ đúng dòng iPad",
                    880)
                : null;
        }

        if (IsUsbCApplePencil(accessoryText))
        {
            return Candidate(
                accessory,
                variant,
                "Apple Pencil USB-C tương thích với mẫu iPad này",
                980);
        }

        if (IsApplePencilPro(accessoryText))
        {
            return SupportsApplePencilPro(profile)
                ? Candidate(
                    accessory,
                    variant,
                    "Apple Pencil Pro tương thích với mẫu iPad này",
                    1_000)
                : null;
        }

        if (IsIpadKeyboard(accessoryText))
        {
            return IsIpadModelMatch(accessoryText, profile)
                ? Candidate(
                    accessory,
                    variant,
                    "Bàn phím đúng dòng và kích thước iPad",
                    960)
                : null;
        }

        if (IsIpadCase(accessoryText))
        {
            return IsIpadModelMatch(accessoryText, profile)
                ? Candidate(
                    accessory,
                    variant,
                    "Phụ kiện bảo vệ đúng dòng và kích thước iPad",
                    940)
                : null;
        }

        if (IsAirPods(accessoryText))
        {
            return Candidate(
                accessory,
                variant,
                "Kết nối nhanh với hệ sinh thái Apple",
                700);
        }

        if (IsUsbCEarPods(accessoryText))
        {
            return Candidate(
                accessory,
                variant,
                "Tai nghe USB-C dùng trực tiếp với iPad",
                680);
        }

        if (IsUsbCCharger(accessoryText))
        {
            return Candidate(
                accessory,
                variant,
                "Sạc USB-C phù hợp với iPad",
                620);
        }

        if (IsUsbCToUsbCCable(accessoryText))
        {
            return Candidate(
                accessory,
                variant,
                "Cáp USB-C phù hợp với iPad",
                600);
        }

        return null;
    }

    private RecommendationCandidate Candidate(
        CatalogProduct accessory,
        e_commerce_web_customer.Models.Entities.ProductVariant variant,
        string memberOffer,
        int score)
    {
        return new RecommendationCandidate(
            accessory,
            variant,
            memberOffer,
            score,
            Name);
    }

    private static bool IsIpadModelMatch(string accessoryText, IpadProfile profile)
    {
        if (!accessoryText.Contains("ipad", StringComparison.Ordinal))
        {
            return false;
        }

        var supportedFamilies = ResolveSupportedFamilies(accessoryText);
        if (supportedFamilies.Count > 0
            && !supportedFamilies.Contains(profile.Family))
        {
            return false;
        }

        var supportedSizes = ResolveSupportedSizes(accessoryText);
        if (supportedSizes.Count > 0
            && profile.Size is not null
            && !supportedSizes.Any(size => AreEquivalentSizes(size, profile.Size)))
        {
            return false;
        }

        var supportedChips = Regex.Matches(accessoryText, "\\bm[2-5]\\b")
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
        if (supportedChips.Count > 0
            && profile.Chip is not null
            && !supportedChips.Contains(profile.Chip))
        {
            return false;
        }

        return true;
    }

    private static bool SupportsApplePencilPro(IpadProfile profile)
    {
        if (profile.Family == "air")
        {
            return profile.Chip is "m2" or "m3" or "m4";
        }

        if (profile.Family == "pro")
        {
            return profile.Chip is "m4" or "m5";
        }

        return profile.Family == "mini"
            && profile.NormalizedText.Contains("mini 7", StringComparison.Ordinal);
    }

    private static IpadProfile? ResolveIpadProfile(string value)
    {
        var normalized = RecommendationTextNormalizer.Normalize(value);
        if (!normalized.Contains("ipad", StringComparison.Ordinal))
        {
            return null;
        }

        return new IpadProfile(
            ResolveFamily(normalized) ?? "base",
            ResolveSize(normalized),
            ResolveChip(normalized),
            normalized);
    }

    private static string? ResolveFamily(string value)
    {
        if (value.Contains("ipad air", StringComparison.Ordinal)) return "air";
        if (value.Contains("ipad pro", StringComparison.Ordinal)) return "pro";
        if (value.Contains("ipad mini", StringComparison.Ordinal)) return "mini";
        return value.Contains("ipad", StringComparison.Ordinal) ? "base" : null;
    }

    private static HashSet<string> ResolveSupportedFamilies(string value)
    {
        var families = new HashSet<string>(StringComparer.Ordinal);
        if (Regex.IsMatch(value, "\\bair\\b")) families.Add("air");
        if (Regex.IsMatch(value, "\\bpro\\b")) families.Add("pro");
        if (Regex.IsMatch(value, "\\bmini\\b")) families.Add("mini");
        if (families.Count == 0 && value.Contains("ipad", StringComparison.Ordinal))
        {
            families.Add("base");
        }

        return families;
    }

    private static string? ResolveSize(string value)
    {
        var match = Regex.Match(value, "\\b(?<size>10 2|10 5|11|12 9|13)\\s*(?:inch)?\\b");
        return match.Success
            ? match.Groups["size"].Value.Replace(' ', '.')
            : null;
    }

    private static HashSet<string> ResolveSupportedSizes(string value)
    {
        return Regex.Matches(value, "\\b(?<size>10 2|10 5|11|12 9|13)\\s*(?:inch)?\\b")
            .Select(match => match.Groups["size"].Value.Replace(' ', '.'))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? ResolveChip(string value)
    {
        var match = Regex.Match(value, "\\b(?<chip>m[2-5])\\b");
        return match.Success ? match.Groups["chip"].Value : null;
    }

    private static bool AreEquivalentSizes(string first, string second)
    {
        return string.Equals(first, second, StringComparison.Ordinal)
            || (first == "12.9" && second == "13")
            || (first == "13" && second == "12.9");
    }

    private static bool IsAppleCare(string text) =>
        text.Contains("applecare", StringComparison.Ordinal)
        || text.Contains("apple care", StringComparison.Ordinal);

    private static bool IsUsbCApplePencil(string text) =>
        text.Contains("apple pencil", StringComparison.Ordinal)
        && (text.Contains("usb c", StringComparison.Ordinal)
            || text.Contains("type c", StringComparison.Ordinal));

    private static bool IsApplePencilPro(string text) =>
        text.Contains("apple pencil pro", StringComparison.Ordinal);

    private static bool IsIpadKeyboard(string text) =>
        text.Contains("ipad", StringComparison.Ordinal)
        && (text.Contains("ban phim", StringComparison.Ordinal)
            || text.Contains("keyboard", StringComparison.Ordinal));

    private static bool IsIpadCase(string text) =>
        text.Contains("ipad", StringComparison.Ordinal)
        && (text.Contains("op lung", StringComparison.Ordinal)
            || text.Contains("bao da", StringComparison.Ordinal)
            || text.Contains("smart cover", StringComparison.Ordinal));

    private static bool IsAirPods(string text) =>
        text.Contains("airpods", StringComparison.Ordinal);

    private static bool IsUsbCEarPods(string text) =>
        text.Contains("earpods", StringComparison.Ordinal)
        && (text.Contains("usb c", StringComparison.Ordinal)
            || text.Contains("type c", StringComparison.Ordinal));

    private static bool IsUsbCCharger(string text)
    {
        return (text.Contains("sac", StringComparison.Ordinal)
                || text.Contains("charger", StringComparison.Ordinal)
                || text.Contains("adapter", StringComparison.Ordinal))
            && (text.Contains("usb c", StringComparison.Ordinal)
                || text.Contains("type c", StringComparison.Ordinal));
    }

    private static bool IsUsbCToUsbCCable(string text)
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

    private sealed record IpadProfile(
        string Family,
        string? Size,
        string? Chip,
        string NormalizedText);
}
