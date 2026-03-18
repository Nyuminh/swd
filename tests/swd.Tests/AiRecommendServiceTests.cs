using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Settings;

namespace swd.Tests;

public class AiRecommendServiceTests
{
    [Fact]
    public async Task RecommendAsync_ShouldUseUserPromptContractAndMapDetailedResponse()
    {
        var repository = new InMemoryProductRepository(new List<Product>
        {
            new()
            {
                Id = "product-1",
                Name = "Classic Round",
                ProductType = "Frame",
                Price = 120m,
                Size = "M",
                Color = "Black",
                TargetGender = "Unisex",
                Inventory = new InventoryInfo { Quantity = 4 },
                Images = new List<ProductImage> { new() { Url = "https://cdn.example.com/1.jpg" } },
                FrameDetails = new FrameDetails
                {
                    FrameShape = "round",
                    FitType = "regular",
                    StyleTags = new List<string> { "classic", "minimal" },
                    FrameMaterial = "metal"
                },
                Warranty = new WarrantyInfo { Months = 12 }
            },
            new()
            {
                Id = "product-2",
                Name = "Navigator Pro",
                ProductType = "Frame",
                Price = 150m,
                Size = "L",
                Color = "Gold",
                TargetGender = "Unisex",
                Inventory = new InventoryInfo { Quantity = 7 },
                Images = new List<ProductImage> { new() { Url = "https://cdn.example.com/2.jpg" } },
                FrameDetails = new FrameDetails
                {
                    FrameShape = "aviator",
                    FitType = "wide",
                    StyleTags = new List<string> { "bold", "luxury" },
                    FrameMaterial = "metal"
                },
                Warranty = new WarrantyInfo { Months = 12 }
            },
            new()
            {
                Id = "product-lens-1",
                Name = "Blue Light Lens",
                ProductType = "Lens",
                Price = 80m,
                Size = "Standard",
                Color = "Clear",
                TargetGender = "Unisex",
                Inventory = new InventoryInfo { Quantity = 12 },
                Images = new List<ProductImage> { new() { Url = "https://cdn.example.com/lens.jpg" } },
                LensDetails = new LensDetails
                {
                    LensType = "blue-light",
                    Index = "1.56",
                    Coatings = new List<string> { "uv", "blue-light" }
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
                            "text": "{\"analysis\":{\"face_shape\":\"Oval\",\"confidence_score\":0.86,\"facial_features\":{\"face_ratio\":\"1:1.35 width:height\",\"jawline\":\"soft\",\"forehead\":\"medium\",\"cheekbones\":\"medium\",\"eye_spacing\":\"average\",\"nose_bridge\":\"medium\"},\"skin_tone\":\"neutral\",\"style_personality\":\"Classic\"},\"recommendations\":[{\"productId\":\"product-2\",\"rank\":1,\"frame_style\":\"Aviator kim loai mong\",\"frame_shape\":\"Aviator\",\"why_suits_you\":\"Dang gong nay giup khuon mat can doi va tao duong net thanh thoat.\",\"frame_width\":\"wide\",\"bridge_type\":\"adjustable\",\"temple_style\":\"standard\",\"material_suggestion\":\"Metal\",\"color_recommendations\":[{\"color\":\"Gold\",\"hex\":\"#D4AF37\",\"reason\":\"Sac vang kim loai giup tong the sang va de phoi do.\"}],\"size_guide\":{\"lens_width_mm\":\"52-55mm\",\"bridge_mm\":\"17-19mm\",\"temple_length_mm\":\"140-145mm\"},\"brands_examples\":[\"Ray-Ban\",\"Bolon\",\"Parim\"],\"price_range\":\"Mid: 500k-2M VND\",\"occasions\":[\"Cong so\",\"Hang ngay\"],\"match_score\":92},{\"productId\":\"product-1\",\"rank\":2,\"frame_style\":\"Round wire toi gian\",\"frame_shape\":\"Round\",\"why_suits_you\":\"Mau nay hop khi muon tong the mem mai hon.\",\"frame_width\":\"medium\",\"bridge_type\":\"keyhole\",\"temple_style\":\"standard\",\"material_suggestion\":\"Metal\",\"color_recommendations\":[{\"color\":\"Black\",\"hex\":\"#000000\",\"reason\":\"Mau den an toan va de dung hang ngay.\"}],\"size_guide\":{\"lens_width_mm\":\"49-51mm\",\"bridge_mm\":\"19-21mm\",\"temple_length_mm\":\"140-145mm\"},\"brands_examples\":[\"Molsion\",\"Bolon\",\"Parim\"],\"price_range\":\"Mid: 500k-2M VND\",\"occasions\":[\"Casual\"],\"match_score\":85}],\"frames_to_avoid\":[{\"frame_style\":\"Round oversize\",\"reason\":\"Dang tron ban lon de lam tong the mat trong hon va giam do can doi.\"},{\"frame_style\":\"Khung qua day ban rong\",\"reason\":\"Gong qua day co the che mat duong net tu nhien cua khuon mat.\"}],\"styling_tips\":[\"Uu tien gong co cau truc gon gang o phan canh tren.\",\"Chon gong co do rong vua den hoi rong hon xuong go ma.\",\"Neu chup anh selfie, tranh goc qua cao de AI nhan dien mat tot hon.\"],\"summary\":\"Khuon mat phu hop voi cac mau gong co duong net ro, dac biet la aviator va rectangle mong. Nen uu tien gong can doi ti le chieu rong khuon mat va giu tong the thanh lich.\"}"
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

        var service = new AiRecommendService(
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

        var response = await service.RecommendAsync(new byte[] { 1, 2, 3, 4 }, "image/jpeg", 3);

        Assert.Equal("Oval", response.Analysis.FaceShape);
        Assert.Equal(0.86m, response.Analysis.ConfidenceScore);
        Assert.Equal("neutral", response.Analysis.SkinTone);
        Assert.Equal("Classic", response.Analysis.StylePersonality);
        Assert.Equal("1:1.35 width:height", response.Analysis.FacialFeatures.FaceRatio);
        Assert.Single(response.Recommendations, x => x.ProductId == "product-2");
        Assert.Equal(1, response.Recommendations[0].Rank);
        Assert.Equal("product-2", response.Recommendations[0].ProductId);
        Assert.Equal("Navigator Pro", response.Recommendations[0].ProductName);
        Assert.Equal("Aviator kim loai mong", response.Recommendations[0].FrameStyle);
        Assert.Equal("52-55mm", response.Recommendations[0].SizeGuide.LensWidthMm);
        Assert.Equal("Ray-Ban", response.Recommendations[0].BrandsExamples[0]);
        Assert.Equal("Mid: 500k-2M VND", response.Recommendations[0].PriceRange);
        Assert.Equal(92, response.Recommendations[0].MatchScore);
        Assert.Equal("https://cdn.example.com/2.jpg", response.Recommendations[0].ImageUrls[0]);
        Assert.Equal(2, response.FramesToAvoid.Count);
        Assert.Equal(3, response.StylingTips.Count);
        Assert.Contains("Khuon mat phu hop", response.Summary);

        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent", handler.LastRequestUri);
        Assert.Equal("gemini-secret", handler.LastApiKey);
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("analysis", handler.LastRequestBody!);
        Assert.Contains("skin_tone", handler.LastRequestBody);
        Assert.Contains("style_personality", handler.LastRequestBody);
        Assert.DoesNotContain("bridge_type", handler.LastRequestBody);
        Assert.DoesNotContain("temple_style", handler.LastRequestBody);
        Assert.Contains("size_guide", handler.LastRequestBody);
        Assert.Contains("brands_examples", handler.LastRequestBody);
        Assert.Contains("frames_to_avoid", handler.LastRequestBody);
        Assert.Contains("styling_tips", handler.LastRequestBody);
        Assert.Contains("summary", handler.LastRequestBody);
        Assert.Contains("Treat frameShape, fitType, styleTags, and frameMaterial from each candidate product as the catalog source of truth.", handler.LastRequestBody);
        Assert.Contains("frameShape must drive frame_shape, fitType must drive frame_width", handler.LastRequestBody);
        Assert.Contains("fitType must drive frame_width", handler.LastRequestBody);
        Assert.Contains("frameMaterial must drive material_suggestion", handler.LastRequestBody);
        Assert.Contains("styleTags must be referenced in why_suits_you", handler.LastRequestBody);
        Assert.Contains("Do not invent frame attributes that are not present in the selected candidate product.", handler.LastRequestBody);
        Assert.DoesNotContain("product-lens-1", handler.LastRequestBody);
        Assert.DoesNotContain("Blue Light Lens", handler.LastRequestBody);
    }

    [Fact]
    public async Task RecommendAsync_ShouldThrowWhenApiKeyMissing()
    {
        var service = new AiRecommendService(
            new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            Options.Create(new GeminiSettings()),
            new InMemoryProductRepository());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecommendAsync(new byte[] { 1, 2, 3 }, "image/png", 3));

        Assert.Equal("Gemini API key is not configured.", exception.Message);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public string? LastRequestBody { get; private set; }
        public string? LastRequestUri { get; private set; }
        public string? LastApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            LastApiKey = request.Headers.TryGetValues("x-goog-api-key", out var values)
                ? values.SingleOrDefault()
                : null;
            LastRequestBody = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

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
}



