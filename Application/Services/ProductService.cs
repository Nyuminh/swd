using swd.Application.DTOs.Product;
using swd.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<Product> CreateAsync(CreateProductRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                CategoryId = request.CategoryId,
                ProductType = request.ProductType,
                Price = request.Price,
                Size = request.Size,
                Color = request.Color,
                TargetGender = request.TargetGender,
                Inventory = new InventoryInfo { Quantity = request.InventoryQuantity },
                Images = request.ImageUrls?.Select(url => new ProductImage { Url = url }).ToList() ?? new List<ProductImage>(),
                Warranty = new WarrantyInfo { Months = request.WarrantyMonths },
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };

            await _repo.CreateAsync(product);
            return product;
        }
    }
}