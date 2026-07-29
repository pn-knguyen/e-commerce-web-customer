using e_commerce_web_customer.Application.Products;
using e_commerce_web_customer.Application.Search;
using e_commerce_web_customer.Application.Search.ContentBased;

namespace e_commerce_web_customer.Infrastructure.Search.ContentBased;

public sealed class ContentBasedSearchRanker : IContentBasedSearchRanker
{
    public ContentBasedSearchQuery CreateQuery(string? query)
    {
        return ContentBasedSearchQuery.Create(query);
    }

    public IReadOnlyList<ContentBasedSearchResult> Rank(
        IEnumerable<ProductReadModel> products,
        string? query,
        int? limit = null)
    {
        var profile = CreateQuery(query);
        var ranked = products
            .Select(product => new ContentBasedSearchResult(
                product,
                Score(profile, ContentBasedSearchDocument.FromProduct(product))))
            .Where(item => item.Score.IsMatch)
            .OrderByDescending(item => item.Score.Total)
            .ThenByDescending(item => item.Score.Text)
            .ThenByDescending(item => item.Product.PopularityScore)
            .ThenBy(item => item.Product.CurrentPrice)
            .ThenBy(item => item.Product.Name)
            .AsEnumerable();

        if (limit is > 0)
        {
            ranked = ranked.Take(limit.Value);
        }

        return ranked.ToList();
    }

    public ContentBasedSearchScore Score(
        ContentBasedSearchQuery query,
        ContentBasedSearchDocument document)
    {
        var popularityScore = CalculatePopularityScore(document);
        if (!query.HasQuery)
        {
            var emptyScore = popularityScore + (document.IsAvailable ? 60 : -40);
            return new ContentBasedSearchScore(
                emptyScore,
                Text: 0,
                Price: 0,
                popularityScore,
                MatchedTermCount: 0,
                IsExactMatch: false,
                IsPriceMatch: true,
                IsMatch: true);
        }

        var textScore = CalculateTextScore(
            query,
            document,
            out var matchedTermCount,
            out var isExactMatch);
        var priceScore = CalculatePriceScore(
            query,
            document.CurrentPrice,
            out var isPriceMatch);
        var availabilityScore = document.IsAvailable ? 70 : -80;
        var ratingScore = document.Rating.HasValue
            ? Math.Min((double)document.Rating.Value * 8, 45)
            : 0;
        var isTextMatch = textScore > 0 || isExactMatch;
        var isPriceOnlyQuery = query.SignificantTerms.Count == 0
            && (query.HasExplicitPrice || query.WantsCheap || query.WantsPremium);
        var isMatch = isPriceMatch
            && (isTextMatch || isPriceOnlyQuery);
        var total = textScore
            + priceScore
            + popularityScore
            + availabilityScore
            + ratingScore;

        return new ContentBasedSearchScore(
            total,
            textScore,
            priceScore,
            popularityScore,
            matchedTermCount,
            isExactMatch,
            isPriceMatch,
            isMatch);
    }

    private static double CalculateTextScore(
        ContentBasedSearchQuery query,
        ContentBasedSearchDocument document,
        out int matchedTermCount,
        out bool isExactMatch)
    {
        var normalizedName = Normalize(document.Name);
        var normalizedBrand = Normalize($"{document.BrandName} {document.BrandSlug}");
        var normalizedCategory = Normalize($"{document.CategoryName} {document.CategorySlug}");
        var normalizedAliases = document.Aliases
            .Select(Normalize)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var normalizedSearchText = Normalize(string.Join(
            ' ',
            new[] { document.Id, document.SearchText }
                .Concat(document.Specifications)));
        var normalizedQuery = query.NormalizedQuery;
        var compactQuery = Compact(normalizedQuery);
        var compactName = Compact(normalizedName);
        var compactSearchText = Compact(normalizedSearchText);
        var score = 0d;
        matchedTermCount = 0;
        isExactMatch = false;

        if (normalizedAliases.Any(alias => alias == normalizedQuery || Compact(alias) == compactQuery)
            || Normalize(document.Id) == normalizedQuery
            || Compact(Normalize(document.Id)) == compactQuery)
        {
            score += 2_400;
            isExactMatch = true;
        }

        if (normalizedName == normalizedQuery)
        {
            score += 1_900;
            isExactMatch = true;
        }
        else if (normalizedName.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            score += 1_350;
        }
        else if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 950;
        }

        if (compactQuery.Length >= 3
            && (compactName.Contains(compactQuery, StringComparison.Ordinal)
                || compactSearchText.Contains(compactQuery, StringComparison.Ordinal)))
        {
            score += 650;
        }

        if (!string.IsNullOrWhiteSpace(normalizedBrand))
        {
            if (normalizedBrand == normalizedQuery)
            {
                score += 650;
            }
            else if (normalizedBrand.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                score += 320;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            if (normalizedCategory == normalizedQuery)
            {
                score += 520;
            }
            else if (normalizedCategory.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                score += 260;
            }
        }

        score += CalculatePreferredCategoryScore(query, normalizedCategory);

        foreach (var term in query.SignificantTerms)
        {
            var termScore = ScoreTerm(
                term,
                normalizedName,
                normalizedBrand,
                normalizedCategory,
                normalizedSearchText,
                normalizedAliases);

            if (termScore <= 0)
            {
                continue;
            }

            matchedTermCount++;
            score += termScore;
        }

        if (query.SignificantTerms.Count > 0
            && matchedTermCount == query.SignificantTerms.Count)
        {
            score += Math.Min(360, query.SignificantTerms.Count * 90);
        }

        return score;
    }

    private static double CalculatePreferredCategoryScore(
        ContentBasedSearchQuery query,
        string normalizedCategory)
    {
        if (query.PreferredCategoryTerms.Count == 0
            || string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return 0;
        }

        return query.PreferredCategoryTerms.Any(term => MatchesNormalizedTerm(normalizedCategory, term))
            ? 850
            : -260;
    }

    private static double ScoreTerm(
        string term,
        string normalizedName,
        string normalizedBrand,
        string normalizedCategory,
        string normalizedSearchText,
        IReadOnlyList<string> normalizedAliases)
    {
        if (normalizedAliases.Any(alias => alias == term || Compact(alias) == term))
        {
            return 620;
        }

        if (ContainsToken(normalizedName, term))
        {
            return 260;
        }

        if (normalizedName.Contains(term, StringComparison.Ordinal))
        {
            return 190;
        }

        if (ContainsToken(normalizedBrand, term)
            || normalizedBrand.Contains(term, StringComparison.Ordinal))
        {
            return 170;
        }

        if (ContainsToken(normalizedCategory, term)
            || normalizedCategory.Contains(term, StringComparison.Ordinal))
        {
            return 155;
        }

        if (ContainsToken(normalizedSearchText, term))
        {
            return 95;
        }

        return normalizedSearchText.Contains(term, StringComparison.Ordinal)
            ? 60
            : 0;
    }

    private static bool MatchesNormalizedTerm(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var normalizedTerm = Normalize(term);
        if (normalizedTerm.Length == 0)
        {
            return false;
        }

        var compactTerm = Compact(normalizedTerm);
        return text.Contains(normalizedTerm, StringComparison.Ordinal)
            || Compact(text).Contains(compactTerm, StringComparison.Ordinal);
    }

    private static double CalculatePriceScore(
        ContentBasedSearchQuery query,
        decimal price,
        out bool isPriceMatch)
    {
        isPriceMatch = true;
        var score = 0d;

        if (query.MinPrice.HasValue && price < query.MinPrice.Value)
        {
            isPriceMatch = false;
            return -1_000;
        }

        if (query.MaxPrice.HasValue && price > query.MaxPrice.Value)
        {
            isPriceMatch = false;
            return -1_000;
        }

        if (query.MinPrice.HasValue)
        {
            score += 180;
        }

        if (query.MaxPrice.HasValue)
        {
            var ratio = query.MaxPrice.Value == 0
                ? 1
                : Math.Clamp((double)(price / query.MaxPrice.Value), 0, 1);
            score += 220 + (1 - ratio) * 90;
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue)
        {
            score += 120;
        }

        if (query.WantsCheap)
        {
            score += price switch
            {
                <= 3_000_000m => 210,
                <= 7_000_000m => 170,
                <= 12_000_000m => 110,
                <= 20_000_000m => 50,
                _ => 0
            };
        }

        if (query.WantsPremium)
        {
            score += price switch
            {
                >= 30_000_000m => 130,
                >= 20_000_000m => 90,
                >= 12_000_000m => 50,
                _ => 0
            };
        }

        return score;
    }

    private static double CalculatePopularityScore(ContentBasedSearchDocument document)
    {
        var popularity = Math.Min(document.PopularityScore / 1_000d, 220);
        if (document.PopularityScore <= 0)
        {
            return document.IsAvailable ? 20 : 0;
        }

        return popularity;
    }

    private static bool ContainsToken(string text, string token)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(item => item == token);
    }

    private static string Normalize(string value)
    {
        return SearchTextNormalizer.Normalize(value ?? string.Empty);
    }

    private static string Compact(string value)
    {
        return value.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
