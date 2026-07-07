using e_commerce_web_customer.Infrastructure.Home.Content;
using e_commerce_web_customer.ViewModels.Shared;

namespace e_commerce_web_customer.Infrastructure.Home.Mock;

internal static class AccessoryDirectoryFactory
{
    public static CategoryDirectoryViewModel Create()
    {
        return new CategoryDirectoryViewModel
        {
            Id = "quality-accessories",
            Title = HomeAccessoryDirectoryContent.Title,
            ViewAllUrl = HomeAccessoryDirectoryContent.MockViewAllUrl,
            Items = HomeAccessoryDirectoryContent.Categories
                .Select(Item)
                .ToList()
        };

        CategoryDirectoryItemViewModel Item(HomeAccessoryCategoryDefinition category)
        {
            return new CategoryDirectoryItemViewModel
            {
                Label = category.MockLabel,
                Url = $"/catalog?cat=accessories&type={category.MockTypeSlug}",
                ImageUrl = HomeAccessoryDirectoryContent.GetMockImageUrl(category),
                ImageAlt = category.MockLabel
            };
        }
    }
}
