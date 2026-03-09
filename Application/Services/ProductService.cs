using swd.Application.DTOs.Product;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Product>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<Product> GetByIdAsync(string id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product is null)
                throw new KeyNotFoundException($"Product with id '{id}' was not found.");

            return product;
        }

        public async Task<List<Product>> GetByCategoryAsync(string categoryId)
            => await _repo.GetByCategoryAsync(categoryId);

        public async Task<Product> CreateAsync(CreateProductRequest request)
        {
            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = request.Name,
                CategoryId = request.CategoryId,
                ProductType = request.ProductType,
                Price = request.Price,
                Size = request.Size,
                Color = request.Color,
                TargetGender = request.TargetGender,
                Inventory = new InventoryInfo
                {
                    Quantity = request.InventoryQuantity
                },
                Images = MapImages(request.ImageUrls),
                Warranty = new WarrantyInfo
                {
                    Months = request.WarrantyMonths
                },
                FrameDetails = IsFrameProductType(request.ProductType)
                    ? MapFrameDetails(request.FrameDetails)
                    : null,
                LensDetails = IsLensProductType(request.ProductType)
                    ? MapLensDetails(request.LensDetails)
                    : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repo.CreateAsync(product);
            return product;
        }

        public async Task<Product> UpdateAsync(string id, UpdateProductRequest request)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product is null)
                throw new KeyNotFoundException($"Product with id '{id}' was not found.");

            product.Name = request.Name;
            product.CategoryId = request.CategoryId;
            product.ProductType = request.ProductType;
            product.Price = request.Price;
            product.Size = request.Size;
            product.Color = request.Color;
            product.TargetGender = request.TargetGender;
            product.Inventory = product.Inventory ?? new InventoryInfo();
            product.Inventory.Quantity = request.InventoryQuantity;

            if (request.ImageUrls is not null)
            {
                product.Images = MapImages(request.ImageUrls);
            }

            product.Warranty = product.Warranty ?? new WarrantyInfo();
            product.Warranty.Months = request.WarrantyMonths;
            product.FrameDetails = IsFrameProductType(request.ProductType)
                ? MapFrameDetails(request.FrameDetails)
                : null;
            product.LensDetails = IsLensProductType(request.ProductType)
                ? MapLensDetails(request.LensDetails)
                : null;
            product.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(id, product);
            return product;
        }

        public async Task DeleteAsync(string id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product is null)
                throw new KeyNotFoundException($"Product with id '{id}' was not found.");

            await _repo.DeleteAsync(id);
        }

        private static List<ProductImage> MapImages(IEnumerable<string>? imageUrls)
        {
            return imageUrls?
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => new ProductImage { Url = url.Trim() })
                .ToList() ?? new List<ProductImage>();
        }

        private static FrameDetails? MapFrameDetails(FrameDetailsRequest? request)
        {
            if (request is null)
                return null;

            var styleTags = request.StyleTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToList() ?? new List<string>();

            var frameShape = request.FrameShape?.Trim() ?? string.Empty;
            var fitType = request.FitType?.Trim() ?? string.Empty;
            var frameMaterial = request.FrameMaterial?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(frameShape)
                && string.IsNullOrWhiteSpace(fitType)
                && string.IsNullOrWhiteSpace(frameMaterial)
                && styleTags.Count == 0)
            {
                return null;
            }

            return new FrameDetails
            {
                FrameShape = frameShape,
                FitType = fitType,
                StyleTags = styleTags,
                FrameMaterial = frameMaterial
            };
        }

        private static LensDetails? MapLensDetails(LensDetailsRequest? request)
        {
            if (request is null)
                return null;

            var coatings = request.Coatings?
                .Where(coating => !string.IsNullOrWhiteSpace(coating))
                .Select(coating => coating.Trim())
                .ToList() ?? new List<string>();

            var lensType = request.LensType?.Trim() ?? string.Empty;
            var index = request.Index?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lensType)
                && string.IsNullOrWhiteSpace(index)
                && coatings.Count == 0)
            {
                return null;
            }

            return new LensDetails
            {
                LensType = lensType,
                Index = index,
                Coatings = coatings
            };
        }

        private static bool IsFrameProductType(string? productType)
        {
            return string.Equals(productType, "Frame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(productType, "Glasses", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLensProductType(string? productType)
        {
            return string.Equals(productType, "Lens", StringComparison.OrdinalIgnoreCase);
        }
    }
}
