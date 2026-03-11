using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using swd.Application.DTOs.Product;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Presentation.Controllers;

namespace swd.Tests;

public class ProductUploadApiTests
{
    [Fact]
    public void ProductMultipartFormRequest_ShouldExposeImageFilesCollection()
    {
        var property = typeof(ProductMultipartFormRequest).GetProperty(nameof(ProductMultipartFormRequest.ImageFiles));

        Assert.NotNull(property);
        Assert.Equal(typeof(List<IFormFile>), property!.PropertyType);
    }

    [Fact]
    public void ProductMultipartFormRequest_ShouldExposeLensCoatingsCollection()
    {
        var property = typeof(ProductMultipartFormRequest).GetProperty(nameof(ProductMultipartFormRequest.LensCoatings));

        Assert.NotNull(property);
        Assert.Equal(typeof(List<string>), property!.PropertyType);
    }

    [Fact]
    public void CreateProductRequest_ShouldNotExposeImageFilesCollection()
    {
        var property = typeof(CreateProductRequest).GetProperty("ImageFiles");

        Assert.Null(property);
    }

    [Fact]
    public void UpdateProductRequest_ShouldNotExposeImageFilesCollection()
    {
        var property = typeof(UpdateProductRequest).GetProperty("ImageFiles");

        Assert.Null(property);
    }

    [Fact]
    public void CreateProductRequest_ShouldExposeFrameAndLensDetails()
    {
        var frameProperty = typeof(CreateProductRequest).GetProperty("FrameDetails");
        var lensProperty = typeof(CreateProductRequest).GetProperty("LensDetails");

        Assert.NotNull(frameProperty);
        Assert.Equal(typeof(FrameDetailsRequest), frameProperty!.PropertyType);
        Assert.NotNull(lensProperty);
        Assert.Equal(typeof(LensDetailsRequest), lensProperty!.PropertyType);
    }

    [Fact]
    public void UpdateProductRequest_ShouldExposeFrameAndLensDetails()
    {
        var frameProperty = typeof(UpdateProductRequest).GetProperty("FrameDetails");
        var lensProperty = typeof(UpdateProductRequest).GetProperty("LensDetails");

        Assert.NotNull(frameProperty);
        Assert.Equal(typeof(FrameDetailsRequest), frameProperty!.PropertyType);
        Assert.NotNull(lensProperty);
        Assert.Equal(typeof(LensDetailsRequest), lensProperty!.PropertyType);
    }

    [Fact]
    public void ProductsController_Create_ShouldAcceptMultipartRequestModel()
    {
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Create));
        var parameters = method!.GetParameters();

        Assert.Single(parameters);
        Assert.Equal(typeof(ProductMultipartFormRequest), parameters[0].ParameterType);
        Assert.NotNull(parameters[0].GetCustomAttribute<FromFormAttribute>());
    }

    [Fact]
    public void ProductsController_Update_ShouldAcceptRouteIdAndMultipartRequestModel()
    {
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Update));
        var parameters = method!.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(ProductMultipartFormRequest), parameters[1].ParameterType);
        Assert.NotNull(parameters[1].GetCustomAttribute<FromFormAttribute>());
    }

    [Fact]
    public async Task Create_ShouldPersistUploadedImages_WhenImageFilesProvided()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"swd-product-upload-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var repository = new InMemoryProductRepository();
            var service = new ProductService(repository);
            var controller = new ProductsController(service, new FakeWebHostEnvironment(tempRoot))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var result = await controller.Create(new ProductMultipartFormRequest
            {
                Name = "Classic Round",
                CategoryId = "category-1",
                ProductType = "Frame",
                Price = 120m,
                Size = "M",
                Color = "Black",
                TargetGender = "Unisex",
                InventoryQuantity = 8,
                ImageFiles = new List<IFormFile>
                {
                    CreateImageFormFile("frame.jpg", "image/jpeg", "fake-image")
                },
                WarrantyMonths = 12,
                FrameShape = "round",
                FitType = "regular",
                StyleTags = new List<string> { "classic", "minimal" },
                FrameMaterial = "metal",
                LensType = "single-vision",
                LensIndex = "1.60",
                LensCoatings = new List<string> { "anti-glare" }
            });

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var product = Assert.IsType<Product>(createdResult.Value);

            Assert.Single(product.Images);
            Assert.StartsWith("/uploads/products/", product.Images[0].Url);

            var relativePath = product.Images[0].Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var savedFilePath = Path.Combine(tempRoot, "wwwroot", relativePath);
            Assert.True(File.Exists(savedFilePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Create_ShouldPersistUploadedImages_WhenProductTypeIsLens()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"swd-product-upload-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var repository = new InMemoryProductRepository();
            var service = new ProductService(repository);
            var controller = new ProductsController(service, new FakeWebHostEnvironment(tempRoot))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var result = await controller.Create(new ProductMultipartFormRequest
            {
                Name = "Blue Light Lens",
                CategoryId = "category-1",
                ProductType = "Lens",
                Price = 90m,
                Size = "M",
                Color = "Clear",
                TargetGender = "Unisex",
                InventoryQuantity = 10,
                ImageFiles = new List<IFormFile>
                {
                    CreateImageFormFile("lens.jpg", "image/jpeg", "fake-lens-image")
                },
                WarrantyMonths = 12,
                LensType = "blue-light",
                LensIndex = "1.56",
                LensCoatings = new List<string> { "uv", "blue-light" }
            });

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var product = Assert.IsType<Product>(createdResult.Value);

            Assert.Single(product.Images);
            Assert.StartsWith("/uploads/products/", product.Images[0].Url);
            Assert.NotNull(product.LensDetails);
            Assert.Equal(new[] { "uv", "blue-light" }, product.LensDetails!.Coatings);
            Assert.Null(product.FrameDetails);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
    [Fact]
    public async Task CreateAsync_ShouldMapFrameDetails_WhenProductTypeIsFrame()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository);

        var product = await service.CreateAsync(new CreateProductRequest
        {
            Name = "Classic Round",
            CategoryId = "category-1",
            ProductType = "Frame",
            Price = 120m,
            Size = "M",
            Color = "Black",
            TargetGender = "Unisex",
            InventoryQuantity = 8,
            ImageUrls = new List<string> { "https://cdn.example.com/frame.jpg" },
            WarrantyMonths = 12,
            FrameDetails = new FrameDetailsRequest
            {
                FrameShape = "round",
                FitType = "regular",
                StyleTags = new List<string> { "classic", "minimal" },
                FrameMaterial = "metal"
            },
            LensDetails = new LensDetailsRequest
            {
                LensType = "single-vision",
                Index = "1.60",
                Coatings = new List<string> { "anti-glare" }
            }
        });

        Assert.NotNull(product.FrameDetails);
        Assert.Equal("round", product.FrameDetails!.FrameShape);
        Assert.Equal("regular", product.FrameDetails.FitType);
        Assert.Equal(new[] { "classic", "minimal" }, product.FrameDetails.StyleTags);
        Assert.Equal("metal", product.FrameDetails.FrameMaterial);
        Assert.Null(product.LensDetails);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMapLensDetailsAndClearFrameDetails_WhenProductTypeChangesToLens()
    {
        var existingProduct = new Product
        {
            Id = "product-1",
            Name = "Classic Frame",
            CategoryId = "category-1",
            ProductType = "Frame",
            Price = 150m,
            Size = "M",
            Color = "Black",
            TargetGender = "Unisex",
            Inventory = new InventoryInfo { Quantity = 10 },
            Images = new List<ProductImage>(),
            FrameDetails = new FrameDetails
            {
                FrameShape = "square",
                FitType = "regular",
                StyleTags = new List<string> { "bold" },
                FrameMaterial = "acetate"
            },
            Warranty = new WarrantyInfo { Months = 12 }
        };
        var repository = new InMemoryProductRepository(new List<Product> { existingProduct });
        var service = new ProductService(repository);

        var result = await service.UpdateAsync(existingProduct.Id, new UpdateProductRequest
        {
            Name = "Blue Light Lens",
            CategoryId = existingProduct.CategoryId,
            ProductType = "Lens",
            Price = 90m,
            Size = existingProduct.Size,
            Color = "Clear",
            TargetGender = existingProduct.TargetGender,
            InventoryQuantity = existingProduct.Inventory.Quantity,
            WarrantyMonths = existingProduct.Warranty.Months,
            ImageUrls = new List<string>(),
            LensDetails = new LensDetailsRequest
            {
                LensType = "blue-light",
                Index = "1.56",
                Coatings = new List<string> { "uv", "blue-light" }
            }
        });

        Assert.Null(result.FrameDetails);
        Assert.NotNull(result.LensDetails);
        Assert.Equal("blue-light", result.LensDetails!.LensType);
        Assert.Equal("1.56", result.LensDetails.Index);
        Assert.Equal(new[] { "uv", "blue-light" }, result.LensDetails.Coatings);
    }

    [Fact]
    public async Task UpdateAsync_ShouldKeepExistingImages_WhenImageUrlsNotProvided()
    {
        var existingProduct = new Product
        {
            Id = "product-1",
            Name = "Classic Frame",
            CategoryId = "category-1",
            ProductType = "Frame",
            Price = 150m,
            Size = "M",
            Color = "Black",
            TargetGender = "Unisex",
            Inventory = new InventoryInfo { Quantity = 10 },
            Images = new List<ProductImage>
            {
                new() { Url = "/uploads/products/existing-1.jpg" },
                new() { Url = "/uploads/products/existing-2.jpg" }
            },
            Warranty = new WarrantyInfo { Months = 12 },
            FrameDetails = new FrameDetails
            {
                FrameShape = "round",
                FitType = "regular",
                StyleTags = new List<string> { "classic" },
                FrameMaterial = "metal"
            }
        };
        var repository = new InMemoryProductRepository(new List<Product> { existingProduct });
        var service = new ProductService(repository);

        var result = await service.UpdateAsync(existingProduct.Id, new UpdateProductRequest
        {
            Name = existingProduct.Name,
            CategoryId = existingProduct.CategoryId,
            ProductType = existingProduct.ProductType,
            Price = existingProduct.Price,
            Size = existingProduct.Size,
            Color = existingProduct.Color,
            TargetGender = existingProduct.TargetGender,
            InventoryQuantity = existingProduct.Inventory.Quantity,
            WarrantyMonths = existingProduct.Warranty.Months,
            ImageUrls = null,
            FrameDetails = new FrameDetailsRequest
            {
                FrameShape = existingProduct.FrameDetails!.FrameShape,
                FitType = existingProduct.FrameDetails.FitType,
                StyleTags = existingProduct.FrameDetails.StyleTags.ToList(),
                FrameMaterial = existingProduct.FrameDetails.FrameMaterial
            }
        });

        Assert.Equal(2, result.Images.Count);
        Assert.Equal("/uploads/products/existing-1.jpg", result.Images[0].Url);
        Assert.Equal("/uploads/products/existing-2.jpg", result.Images[1].Url);
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public InMemoryProductRepository(List<Product>? seed = null)
        {
            _products = seed ?? new List<Product>();
        }

        public Task<List<Product>> GetAllAsync()
        {
            return Task.FromResult(_products.ToList());
        }

        public Task<Product> GetByIdAsync(string id)
        {
            return Task.FromResult(_products.FirstOrDefault(x => x.Id == id)!);
        }

        public Task CreateAsync(Product entity)
        {
            _products.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Product entity)
        {
            var index = _products.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                _products[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _products.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<Product>> GetByCategoryAsync(string categoryId)
        {
            return Task.FromResult(_products.Where(x => x.CategoryId == categoryId).ToList());
        }

        public Task<bool> TryReserveInventoryAsync(string id, int quantity)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product?.Inventory is null || product.Inventory.Quantity < quantity)
            {
                return Task.FromResult(false);
            }

            product.Inventory.Quantity -= quantity;
            return Task.FromResult(true);
        }

        public Task ReleaseInventoryAsync(string id, int quantity)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product?.Inventory is not null)
            {
                product.Inventory.Quantity += quantity;
            }

            return Task.CompletedTask;
        }
    }

    private static IFormFile CreateImageFormFile(string fileName, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "imageFiles", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new NullFileProvider();
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
            WebRootFileProvider = new NullFileProvider();
            ApplicationName = "swd.Tests";
            EnvironmentName = "Development";
        }

        public string ApplicationName { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        public string WebRootPath { get; set; }

        public string EnvironmentName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}

