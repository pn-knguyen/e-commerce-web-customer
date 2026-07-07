namespace e_commerce_web_customer.Application.Recommendations.Algorithms;

public sealed class ProductAssociationRule
{
    public required string SourceSlug { get; init; }
    public required string TargetSlug { get; init; }
    public double Confidence { get; init; }
    public double Support { get; init; }
}

public sealed class AprioriAlgorithm
{
    private readonly double _minSupport;
    private readonly double _minConfidence;

    public AprioriAlgorithm(double minSupport = 0.05, double minConfidence = 0.2)
    {
        _minSupport = minSupport;
        _minConfidence = minConfidence;
    }

    public IReadOnlyList<ProductAssociationRule> GenerateRules(
        IReadOnlyList<HashSet<string>> transactions)
    {
        if (transactions.Count == 0)
        {
            return [];
        }

        var totalTransactions = transactions.Count;
        var minSupportCount = Math.Max(
            1,
            (int)Math.Ceiling(_minSupport * totalTransactions));

        var itemCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var transaction in transactions)
        {
            foreach (var item in transaction)
            {
                itemCounts[item] = itemCounts.GetValueOrDefault(item) + 1;
            }
        }

        var frequentItems = itemCounts
            .Where(item => item.Value >= minSupportCount)
            .ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.OrdinalIgnoreCase);

        if (frequentItems.Count < 2)
        {
            return [];
        }

        var pairCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var transaction in transactions)
        {
            var frequentTransactionItems = transaction
                .Where(frequentItems.ContainsKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (var i = 0; i < frequentTransactionItems.Count; i++)
            {
                for (var j = i + 1; j < frequentTransactionItems.Count; j++)
                {
                    var pairKey = BuildPairKey(
                        frequentTransactionItems[i],
                        frequentTransactionItems[j]);
                    pairCounts[pairKey] = pairCounts.GetValueOrDefault(pairKey) + 1;
                }
            }
        }

        var rules = new List<ProductAssociationRule>();
        foreach (var pair in pairCounts)
        {
            if (pair.Value < minSupportCount)
            {
                continue;
            }

            var items = pair.Key.Split('|', 2);
            if (items.Length != 2)
            {
                continue;
            }

            AddRuleIfConfident(
                rules,
                sourceSlug: items[0],
                targetSlug: items[1],
                pairCount: pair.Value,
                sourceCount: frequentItems[items[0]],
                totalTransactions);
            AddRuleIfConfident(
                rules,
                sourceSlug: items[1],
                targetSlug: items[0],
                pairCount: pair.Value,
                sourceCount: frequentItems[items[1]],
                totalTransactions);
        }

        return rules
            .OrderByDescending(rule => rule.Confidence)
            .ThenByDescending(rule => rule.Support)
            .ThenBy(rule => rule.TargetSlug)
            .ToList();
    }

    private void AddRuleIfConfident(
        List<ProductAssociationRule> rules,
        string sourceSlug,
        string targetSlug,
        int pairCount,
        int sourceCount,
        int totalTransactions)
    {
        var confidence = (double)pairCount / sourceCount;
        if (confidence < _minConfidence)
        {
            return;
        }

        rules.Add(new ProductAssociationRule
        {
            SourceSlug = sourceSlug,
            TargetSlug = targetSlug,
            Confidence = confidence,
            Support = (double)pairCount / totalTransactions
        });
    }

    private static string BuildPairKey(string itemA, string itemB)
    {
        return string.Compare(itemA, itemB, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{itemA}|{itemB}"
            : $"{itemB}|{itemA}";
    }
}
