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
                Images = request.ImageUrls?.Select(url => new ProductImage { Url = url }).ToList() ?? new List<ProductImage>(),
                Warranty = new WarrantyInfo
                {
                    Months = request.WarrantyMonths
                },
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
            product.Images = request.ImageUrls?.Select(url => new ProductImage { Url = url }).ToList() ?? new List<ProductImage>();
            product.Warranty = product.Warranty ?? new WarrantyInfo();
            product.Warranty.Months = request.WarrantyMonths;
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
    }
}
