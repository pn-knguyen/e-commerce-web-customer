using e_commerce_web_customer.Application.Products;

namespace e_commerce_web_customer.Application.Search.ContentBased;

public interface IContentBasedSearchRanker
{
    ContentBasedSearchQuery CreateQuery(string? query);

    ContentBasedSearchScore Score(
        ContentBasedSearchQuery query,
        ContentBasedSearchDocument document);

    IReadOnlyList<ContentBasedSearchResult> Rank(
        IEnumerable<ProductReadModel> products,
        string? query,
        int? limit = null);
}
