using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace e_commerce_web_customer.Infrastructure.Recommendations.Rules;

internal static class RecommendationTextNormalizer
{
    public static string Normalize(string value)
    {
        return Regex.Replace(
            RemoveDiacritics(value).ToLowerInvariant(),
            "[^a-z0-9]+",
            " ")
            .Trim();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
