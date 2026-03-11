using MongoDB.Bson;

namespace swd.Application.Services
{
    internal static class ProductImageUrlResolver
    {
        private static readonly string[] LegacySingleImageKeys = ["ImageUrl", "imageUrl"];
        private static readonly string[] LegacyImageCollectionKeys = ["ImageUrls", "imageUrls", "Images", "images"];

        public static List<string> GetImageUrls(Product product)
        {
            if (product is null)
                return new List<string>();

            var imageUrls = new List<string>();
            AddStructuredImageUrls(imageUrls, product.Images);
            AddLegacyImageUrls(imageUrls, product.ExtraElements);

            return imageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string? GetFirstImageUrl(Product product)
        {
            return GetImageUrls(product).FirstOrDefault();
        }

        private static void AddStructuredImageUrls(List<string> imageUrls, IEnumerable<ProductImage>? images)
        {
            if (images is null)
                return;

            imageUrls.AddRange(images
                .Where(image => !string.IsNullOrWhiteSpace(image?.Url))
                .Select(image => image.Url));
        }

        private static void AddLegacyImageUrls(List<string> imageUrls, BsonDocument? extraElements)
        {
            if (extraElements is null)
                return;

            foreach (var key in LegacySingleImageKeys)
            {
                if (extraElements.TryGetValue(key, out var value))
                {
                    AddBsonImageValues(imageUrls, value);
                }
            }

            foreach (var key in LegacyImageCollectionKeys)
            {
                if (extraElements.TryGetValue(key, out var value))
                {
                    AddBsonImageValues(imageUrls, value);
                }
            }
        }

        private static void AddBsonImageValues(List<string> imageUrls, BsonValue value)
        {
            if (value is null || value.IsBsonNull)
                return;

            if (value.IsString)
            {
                imageUrls.Add(value.AsString);
                return;
            }

            if (value.IsBsonArray)
            {
                foreach (var item in value.AsBsonArray)
                {
                    AddBsonImageValues(imageUrls, item);
                }

                return;
            }

            if (!value.IsBsonDocument)
                return;

            var document = value.AsBsonDocument;
            if (document.TryGetValue("Url", out var urlValue) || document.TryGetValue("url", out urlValue))
            {
                AddBsonImageValues(imageUrls, urlValue);
            }
        }
    }
}
