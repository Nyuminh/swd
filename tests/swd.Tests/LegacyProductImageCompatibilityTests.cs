using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Settings;

namespace swd.Tests;

public class LegacyProductImageCompatibilityTests
{
    [Fact]
    public async Task RecommendAsync_ShouldExposeLegacyImageUrls_WhenStructuredImagesAreMissing()
    {
        var repository = new InMemoryProductRepository(new List<Product>
        {
            new()
            {
                Id = "product-legacy",
                Name = "Legacy Frame",
                ProductType = "Frame",
                Price = 150m,
                Size = "M",
                Color = "Black",
                TargetGender = "Unisex",
                Inventory = new InventoryInfo { Quantity = 7 },
                Images = new List<ProductImage>(),
                ExtraElements = new BsonDocument
                {
                    { "ImageUrls", new BsonArray { "https://cdn.example.com/legacy-frame.jpg" } }
                },
                FrameDetails = new FrameDetails
                {
                    FrameShape = "aviator",
                    FitType = "wide",
                    StyleTags = new List<string> { "bold" },
                    FrameMaterial = "metal"
                },
                Warranty = new WarrantyInfo { Months = 12 }
            }
        });

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "{\"analysis\":{\"face_shape\":\"Oval\",\"confidence_score\":0.86,\"facial_features\":{\"face_ratio\":\"1:1.35 width:height\",\"jawline\":\"soft\",\"forehead\":\"medium\",\"cheekbones\":\"medium\",\"eye_spacing\":\"average\",\"nose_bridge\":\"medium\"},\"skin_tone\":\"neutral\",\"style_personality\":\"Classic\"},\"recommendations\":[{\"productId\":\"product-legacy\",\"rank\":1,\"frame_style\":\"Aviator kim loai mong\",\"frame_shape\":\"Aviator\",\"why_suits_you\":\"Dang gong nay giup khuon mat can doi va tao duong net thanh thoat.\",\"frame_width\":\"wide\",\"bridge_type\":\"adjustable\",\"temple_style\":\"standard\",\"material_suggestion\":\"Metal\",\"color_recommendations\":[{\"color\":\"Gold\",\"hex\":\"#D4AF37\",\"reason\":\"Sac vang kim loai giup tong the sang va de phoi do.\"}],\"size_guide\":{\"lens_width_mm\":\"52-55mm\",\"bridge_mm\":\"17-19mm\",\"temple_length_mm\":\"140-145mm\"},\"brands_examples\":[\"Ray-Ban\"],\"price_range\":\"Mid: 500k-2M VND\",\"occasions\":[\"Cong so\"],\"match_score\":92}],\"frames_to_avoid\":[],\"styling_tips\":[],\"summary\":\"Tom tat.\"}"
                          }
                        ]
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = new GeminiRecommendationService(
            new HttpClient(handler),
            Options.Create(new GeminiSettings
            {
                ApiKey = "gemini-secret",
                Model = "gemini-2.5-flash",
                BaseUrl = "https://generativelanguage.googleapis.com",
                MaxCandidateProducts = 10,
                MaxRecommendations = 5
            }),
            repository);

        var response = await service.RecommendAsync(new byte[] { 1, 2, 3, 4 }, "image/jpeg", 1);

        Assert.Single(response.Recommendations);
        Assert.Equal("https://cdn.example.com/legacy-frame.jpg", response.Recommendations[0].ImageUrls[0]);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public InMemoryProductRepository(List<Product>? products = null)
        {
            _products = products ?? new List<Product>();
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
            return Task.FromResult(true);
        }

        public Task ReleaseInventoryAsync(string id, int quantity)
        {
            return Task.CompletedTask;
        }
    }
}
