using e_commerce_web_customer.Application.Products;

namespace e_commerce_web_customer.Application.Search.ContentBased;

public sealed record ContentBasedSearchScore(
    double Total,
    double Text,
    double Price,
    double Popularity,
    int MatchedTermCount,
    bool IsExactMatch,
    bool IsPriceMatch,
    bool IsMatch);

public sealed record ContentBasedSearchResult(
    ProductReadModel Product,
    ContentBasedSearchScore Score);
