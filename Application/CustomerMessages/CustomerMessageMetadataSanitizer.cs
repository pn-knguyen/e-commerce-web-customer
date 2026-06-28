using System.Text.Json;
using System.Text.Json.Nodes;

namespace e_commerce_web_customer.Application.CustomerMessages;

public static class CustomerMessageMetadataSanitizer
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static string? Sanitize(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            var source = JsonNode.Parse(metadataJson) as JsonObject;
            if (source?["products"] is not JsonArray products)
            {
                return null;
            }

            var safeProducts = new JsonArray();
            foreach (var node in products.OfType<JsonObject>().Take(12))
            {
                var safeProduct = new JsonObject();
                CopyValue(node, safeProduct, "id");
                CopyValue(node, safeProduct, "name");
                CopyValue(node, safeProduct, "price");
                CopyValue(node, safeProduct, "imageUrl");
                CopyValue(node, safeProduct, "categoryName");
                CopyValue(node, safeProduct, "detailUrl");
                safeProducts.Add(safeProduct);
            }

            return new JsonObject
            {
                ["source"] = "customer-ai-assistant",
                ["products"] = safeProducts,
            }.ToJsonString(WebJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CopyValue(JsonObject source, JsonObject target, string propertyName)
    {
        if (source[propertyName] is { } value)
        {
            target[propertyName] = value.DeepClone();
        }
    }
}
