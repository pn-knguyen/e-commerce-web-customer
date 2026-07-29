using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace e_commerce_web_customer.Application.Search.ContentBased;

public sealed class ContentBasedSearchQuery
{
    private static readonly Regex RangePriceRegex = new(
        @$"(?<first>\d+(?:[\.,]\d+)?)\s*(?<firstUnit>{PriceUnitPattern})?\s*(?:-|den|toi|to)\s*(?<second>\d+(?:[\.,]\d+)?)\s*(?<secondUnit>{PriceUnitPattern})?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex MaxPriceRegex = new(
        @$"(?:duoi|toi da|nho hon|khong qua|be hon|<=|<)\s*(?<amount>\d+(?:[\.,]\d+)?)\s*(?<unit>{PriceUnitPattern})?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex MinPriceRegex = new(
        @$"(?:tren|tu|it nhat|lon hon|>=|>)\s*(?<amount>\d+(?:[\.,]\d+)?)\s*(?<unit>{PriceUnitPattern})?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex StandalonePriceRegex = new(
        @$"(?<amount>\d+(?:[\.,]\d+)?)\s*(?<unit>{PriceUnitPattern})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a",
        "ai",
        "ban",
        "bang",
        "ben",
        "bo",
        "can",
        "cao",
        "cap",
        "cho",
        "co",
        "con",
        "cua",
        "dang",
        "den",
        "duoc",
        "duoi",
        "gia",
        "hang",
        "hon",
        "khong",
        "kiem",
        "la",
        "loai",
        "mua",
        "nhat",
        "nhieu",
        "nho",
        "o",
        "phu",
        "qua",
        "re",
        "san",
        "sieu",
        "tam",
        "theo",
        "thich",
        "tim",
        "tot",
        "toi",
        "tr",
        "tren",
        "trieu",
        "tu",
        "vnd",
        "voi"
    };

    private static readonly IReadOnlyDictionary<string, string[]> Synonyms =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["dien"] = ["phone", "smartphone"],
            ["thoai"] = ["phone", "smartphone"],
            ["dt"] = ["dien", "thoai", "phone"],
            ["smartphone"] = ["dien", "thoai"],
            ["laptop"] = ["notebook", "may", "tinh"],
            ["notebook"] = ["laptop"],
            ["tablet"] = ["may", "tinh", "bang"],
            ["ipad"] = ["tablet", "may", "tinh", "bang"],
            ["tv"] = ["tivi"],
            ["tivi"] = ["tv"],
            ["headphone"] = ["tai", "nghe"],
            ["earphone"] = ["tai", "nghe"],
            ["gaming"] = ["game"],
            ["game"] = ["gaming"],
            ["camera"] = ["chup", "anh"],
            ["anh"] = ["camera"],
            ["chup"] = ["camera"],
            ["sac"] = ["charger", "adapter"],
            ["charger"] = ["sac"],
            ["dong"] = ["watch"],
            ["ho"] = ["watch"],
            ["watch"] = ["dong", "ho"]
        };

    private const string PriceUnitPattern = "trieu|tr|m|million|nghin|ngan|k|vnd|dong";

    public string RawQuery { get; private init; } = string.Empty;
    public string NormalizedQuery { get; private init; } = string.Empty;
    public IReadOnlyList<string> Terms { get; private init; } = [];
    public IReadOnlyList<string> SignificantTerms { get; private init; } = [];
    public IReadOnlyList<string> CandidateTerms { get; private init; } = [];
    public IReadOnlyList<string> PreferredCategoryTerms { get; private init; } = [];
    public decimal? MinPrice { get; private init; }
    public decimal? MaxPrice { get; private init; }
    public bool WantsCheap { get; private init; }
    public bool WantsPremium { get; private init; }
    public bool HasExplicitPrice => MinPrice.HasValue || MaxPrice.HasValue;
    public bool HasQuery => !string.IsNullOrWhiteSpace(NormalizedQuery);

    public static ContentBasedSearchQuery Create(string? rawQuery)
    {
        var cleanQuery = SearchTextNormalizer.CleanQuery(rawQuery);
        var normalizedQuery = SearchTextNormalizer.Normalize(cleanQuery);
        var normalizedForPrice = NormalizeForPrice(cleanQuery);
        var priceValues = new HashSet<string>(ExtractPriceValueTerms(normalizedForPrice), StringComparer.Ordinal);
        var priceRange = ParsePriceRange(normalizedForPrice);
        var terms = normalizedQuery
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var significantTerms = terms // từ khóa quan trọng
            .Where(term => IsSignificantTerm(term, priceValues))
            .Take(12)
            .ToList();
        var candidateTerms = ExpandCandidateTerms(significantTerms) // từ khóa mở rộng để tìm rộng hơn
            .Take(20)
            .ToList();
        var preferredCategoryTerms = InferPreferredCategoryTerms(normalizedQuery); // danh mục nên ưu tiên

        return new ContentBasedSearchQuery
        {
            RawQuery = cleanQuery,
            NormalizedQuery = normalizedQuery,
            Terms = terms,
            SignificantTerms = significantTerms,
            CandidateTerms = candidateTerms,
            PreferredCategoryTerms = preferredCategoryTerms,
            MinPrice = priceRange.MinPrice,
            MaxPrice = priceRange.MaxPrice,
            WantsCheap = ContainsAny(normalizedQuery, "gia re", "re", "tiet kiem", "binh dan"),
            WantsPremium = ContainsAny(normalizedQuery, "cao cap", "flagship", "pro", "ultra", "max")
        };
    }

    private static bool IsSignificantTerm(string term, IReadOnlySet<string> priceValues)
    {
        if (term.Length <= 1 && !char.IsDigit(term[0]))
        {
            return false;
        }

        return !StopWords.Contains(term)
            && !priceValues.Contains(term);
    }

    private static IEnumerable<string> ExpandCandidateTerms(IReadOnlyList<string> significantTerms)
    {
        foreach (var term in significantTerms)
        {
            yield return term;

            if (!Synonyms.TryGetValue(term, out var synonyms))
            {
                continue;
            }

            foreach (var synonym in synonyms)
            {
                yield return synonym;
            }
        }
    }

    private static IReadOnlyList<string> InferPreferredCategoryTerms(string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return [];
        }

        var categories = new List<string>();

        if (ContainsAny(
                normalizedQuery,
                "op lung",
                "case",
                "bao da",
                "dan man hinh",
                "kinh cuong luc",
                "cu sac",
                "cap sac",
                "sac",
                "charger",
                "adapter",
                "phu kien",
                "magsafe",
                "apple care",
                "applecare"))
        {
            AddCategoryTerms(
                categories,
                "op lung",
                "phu kien",
                "dan man hinh",
                "sac",
                "case",
                "magsafe",
                "charger",
                "adapter",
                "apple care");

            return categories.Distinct(StringComparer.Ordinal).ToList();
        }

        if (ContainsAny(normalizedQuery, "iphone", "dien thoai", "smartphone", "dt"))
        {
            AddCategoryTerms(categories, "dien thoai", "phone", "smartphone");
        }

        if (ContainsAny(normalizedQuery, "laptop", "macbook", "notebook"))
        {
            AddCategoryTerms(categories, "laptop", "may tinh xach tay", "notebook");
        }

        if (ContainsAny(normalizedQuery, "ipad", "tablet", "may tinh bang"))
        {
            AddCategoryTerms(categories, "may tinh bang", "tablet", "ipad");
        }

        if (ContainsAny(normalizedQuery, "dong ho", "watch", "smartwatch"))
        {
            AddCategoryTerms(categories, "dong ho thong minh", "dong ho", "watch", "smartwatch");
        }

        if (ContainsAny(normalizedQuery, "tai nghe", "headphone", "earphone", "airpods"))
        {
            AddCategoryTerms(categories, "tai nghe", "am thanh", "audio", "headphone", "earphone");
        }

        if (ContainsAny(normalizedQuery, "loa", "speaker"))
        {
            AddCategoryTerms(categories, "loa", "am thanh", "audio", "speaker");
        }

        if (ContainsAny(normalizedQuery, "tivi", "tv"))
        {
            AddCategoryTerms(categories, "tivi", "tv");
        }

        if (ContainsAny(normalizedQuery, "camera", "camera an ninh"))
        {
            AddCategoryTerms(categories, "camera", "camera an ninh");
        }

        return categories.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void AddCategoryTerms(List<string> categories, params string[] terms)
    {
        categories.AddRange(terms
            .Select(SearchTextNormalizer.Normalize)
            .Where(term => term.Length > 0));
    }

    private static bool ContainsAny(string normalizedText, params string[] values)
    {
        return values.Any(value =>
            normalizedText.Contains(
                SearchTextNormalizer.Normalize(value),
                StringComparison.Ordinal));
    }

    private static (decimal? MinPrice, decimal? MaxPrice) ParsePriceRange(string normalizedForPrice)
    {
        decimal? minPrice = null;
        decimal? maxPrice = null;

        var rangeMatch = RangePriceRegex.Match(normalizedForPrice);
        if (rangeMatch.Success
            && TryParseMoney(
                rangeMatch.Groups["first"].Value,
                FirstNonEmpty(
                    rangeMatch.Groups["firstUnit"].Value,
                    rangeMatch.Groups["secondUnit"].Value),
                out var first)
            && TryParseMoney(
                rangeMatch.Groups["second"].Value,
                FirstNonEmpty(
                    rangeMatch.Groups["secondUnit"].Value,
                    rangeMatch.Groups["firstUnit"].Value),
                out var second))
        {
            minPrice = Math.Min(first, second);
            maxPrice = Math.Max(first, second);
        }

        var maxMatch = MaxPriceRegex.Match(normalizedForPrice);
        if (maxMatch.Success
            && TryParseMoney(
                maxMatch.Groups["amount"].Value,
                maxMatch.Groups["unit"].Value,
                out var parsedMax))
        {
            maxPrice = maxPrice.HasValue
                ? Math.Min(maxPrice.Value, parsedMax)
                : parsedMax;
        }

        var minMatch = MinPriceRegex.Match(normalizedForPrice);
        if (minMatch.Success
            && TryParseMoney(
                minMatch.Groups["amount"].Value,
                minMatch.Groups["unit"].Value,
                out var parsedMin))
        {
            minPrice = minPrice.HasValue
                ? Math.Max(minPrice.Value, parsedMin)
                : parsedMin;
        }

        if (!minPrice.HasValue && !maxPrice.HasValue)
        {
            var standaloneMatch = StandalonePriceRegex.Match(normalizedForPrice);
            if (standaloneMatch.Success
                && TryParseMoney(
                    standaloneMatch.Groups["amount"].Value,
                    standaloneMatch.Groups["unit"].Value,
                    out var parsedBudget))
            {
                maxPrice = parsedBudget;
            }
        }

        return (minPrice, maxPrice);
    }

    private static IEnumerable<string> ExtractPriceValueTerms(string normalizedForPrice)
    {
        foreach (Match match in RangePriceRegex.Matches(normalizedForPrice))
        {
            yield return NormalizeNumericTerm(match.Groups["first"].Value);
            yield return NormalizeNumericTerm(match.Groups["second"].Value);
        }

        foreach (Match match in MaxPriceRegex.Matches(normalizedForPrice))
        {
            yield return NormalizeNumericTerm(match.Groups["amount"].Value);
        }

        foreach (Match match in MinPriceRegex.Matches(normalizedForPrice))
        {
            yield return NormalizeNumericTerm(match.Groups["amount"].Value);
        }

        foreach (Match match in StandalonePriceRegex.Matches(normalizedForPrice))
        {
            yield return NormalizeNumericTerm(match.Groups["amount"].Value);
        }
    }

    private static bool TryParseMoney(string value, string? unit, out decimal amount)
    {
        amount = 0;
        if (!decimal.TryParse(
                value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return false;
        }

        var normalizedUnit = SearchTextNormalizer.Normalize(unit ?? string.Empty);
        amount = normalizedUnit switch
        {
            "nghin" or "ngan" or "k" => parsed * 1_000m,
            "vnd" or "dong" when parsed >= 10_000m => parsed,
            _ when parsed <= 500m => parsed * 1_000_000m,
            _ => parsed
        };
        return true;
    }

    private static string NormalizeNumericTerm(string value)
    {
        return value.Replace(',', '.').Split('.')[0];
    }

    private static string FirstNonEmpty(string first, string second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }

    private static string NormalizeForPrice(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalizedCharacter = character == 'đ' ? 'd' : character;
            if (char.IsLetterOrDigit(normalizedCharacter)
                || char.IsWhiteSpace(normalizedCharacter)
                || normalizedCharacter is '.' or ',' or '-' or '<' or '>')
            {
                builder.Append(normalizedCharacter);
            }
        }

        return string.Join(' ', builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
