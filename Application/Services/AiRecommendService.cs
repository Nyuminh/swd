using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using swd.Application.DTOs.Ai;
using swd.Domain.Interfaces;
using swd.Settings;

namespace swd.Application.Services
{
    public class AiRecommendService
    {
        private const int MaxPortraitBytes = 10 * 1024 * 1024;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly IProductRepository _productRepository;

        public AiRecommendService(
            HttpClient httpClient,
            IOptions<GeminiSettings> settings,
            IProductRepository productRepository)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _productRepository = productRepository;
        }

        public async Task<RecommendResponse> RecommendAsync(
            byte[] portraitBytes,
            string mimeType,
            int? maxRecommendations = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            if (portraitBytes.Length == 0)
                throw new InvalidOperationException("Portrait image is empty.");

            if (portraitBytes.Length > MaxPortraitBytes)
                throw new InvalidOperationException("Portrait image exceeds 10 MB limit.");

            if (string.IsNullOrWhiteSpace(mimeType) || !mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Portrait file must be an image.");

            var candidateProducts = await GetCandidateProductsAsync(cancellationToken);
            if (candidateProducts.Count == 0)
                throw new InvalidOperationException("No candidate products are available for AI recommendations.");

            var normalizedMaxRecommendations = NormalizeMaxRecommendations(maxRecommendations);
            var request = BuildRequest(portraitBytes, mimeType, candidateProducts, normalizedMaxRecommendations);
            var endpoint = BuildEndpoint();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey.Trim());

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini API request failed with status {(int)response.StatusCode}: {responseBody}");

            var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseBody, JsonOptions)
                ?? throw new InvalidOperationException("Gemini API returned an empty response.");

            var jsonPayload = geminiResponse.Candidates?
                .SelectMany(candidate => candidate.Content?.Parts ?? Enumerable.Empty<GeminiPartResponse>())
                .Select(part => part.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            if (string.IsNullOrWhiteSpace(jsonPayload))
                throw new InvalidOperationException("Gemini API did not return recommendation content.");

            var modelResponse = JsonSerializer.Deserialize<GeminiPayload>(jsonPayload, JsonOptions)
                ?? throw new InvalidOperationException("Gemini API returned invalid recommendation JSON.");

            return MapToResponse(modelResponse, candidateProducts, normalizedMaxRecommendations);
        }

        private async Task<List<Product>> GetCandidateProductsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allProducts = await _productRepository.GetAllAsync();
            var products = allProducts
                .Where(product => product is not null)
                .Where(IsFrameProduct)
                .Where(product => (product.Inventory?.Quantity ?? 0) > 0)
                .OrderByDescending(product => product.Inventory?.Quantity ?? 0)
                .Take(Math.Max(1, _settings.MaxCandidateProducts))
                .ToList();

            if (products.Count > 0)
                return products;

            return allProducts
                .Where(product => product is not null)
                .Where(IsFrameProduct)
                .Take(Math.Max(1, _settings.MaxCandidateProducts))
                .ToList();
        }

        private object BuildRequest(
            byte[] portraitBytes,
            string mimeType,
            IReadOnlyList<Product> candidateProducts,
            int maxRecommendations)
        {
            return new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiContentRequest
                {
                    Parts = new List<GeminiPartRequest>
                    {
                        new() { Text = BuildSystemInstruction() }
                    }
                },
                Contents = new List<GeminiContentRequest>
                {
                    new()
                    {
                        Parts = new List<GeminiPartRequest>
                        {
                            new()
                            {
                                InlineData = new GeminiInlineDataRequest
                                {
                                    MimeType = mimeType,
                                    Data = Convert.ToBase64String(portraitBytes)
                                }
                            },
                            new()
                            {
                                Text = BuildUserPrompt(candidateProducts, maxRecommendations)
                            }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfigRequest
                {
                    Temperature = 0.2,
                    ResponseMimeType = "application/json",
                    ResponseJsonSchema = BuildResponseSchema(maxRecommendations)
                }
            };
        }

        private string BuildEndpoint()
        {
            return $"{_settings.BaseUrl.TrimEnd('/')}/v1beta/models/{_settings.Model}:generateContent";
        }

        private int NormalizeMaxRecommendations(int? maxRecommendations)
        {
            var defaultMax = _settings.MaxRecommendations <= 0 ? 5 : _settings.MaxRecommendations;
            var requested = maxRecommendations ?? defaultMax;
            return Math.Clamp(requested, 1, defaultMax);
        }

        private static string BuildSystemInstruction()
        {
            return "You are an expert optical consultant AI specializing in eyeglass frame recommendations. Analyze the user's facial features from the uploaded portrait, but treat each candidate product's FrameDetails as the authoritative catalog data for product-specific attributes. Always return valid JSON only. All explanation fields must be in Vietnamese. If a field cannot be determined reliably from the image or product data, return 'unknown' for strings, [] for arrays, or a cautious low confidence score instead of inventing details. Recommend only products from the provided candidate list, include the exact productId for each recommendation, and never invent frame attributes that conflict with the selected product's catalog data.";
        }

        private static string BuildUserPrompt(IReadOnlyList<Product> candidateProducts, int maxRecommendations)
        {
            var productLines = candidateProducts.Select(product =>
                string.Join('\n', new[]
                {
                    $"- productId: {product.Id}",
                    $"  name: {product.Name}",
                    $"  type: {product.ProductType}",
                    $"  price: {product.Price}",
                    $"  size: {product.Size}",
                    $"  color: {product.Color}",
                    $"  targetGender: {product.TargetGender}",
                    $"  stock: {product.Inventory?.Quantity ?? 0}",
                    "  catalog_frame_details:",
                    $"    frameShape: {product.FrameDetails?.FrameShape ?? "unknown"}",
                    $"    fitType: {product.FrameDetails?.FitType ?? "unknown"}",
                    $"    styleTags: {FormatStyleTags(product.FrameDetails?.StyleTags)}",
                    $"    frameMaterial: {product.FrameDetails?.FrameMaterial ?? "unknown"}"
                }));

            return string.Join('\n', new[]
            {
                "You are an expert optical consultant AI specializing in eyeglass frame recommendations.",
                "Analyze the user's facial features from their uploaded photo and recommend suitable glasses frames.",
                string.Empty,
                "## YOUR TASK:",
                "Analyze the face in the provided image and return a structured JSON response with:",
                "1. Face shape detection",
                "2. Key facial measurements/proportions",
                $"3. Top {maxRecommendations} glasses frame recommendations",
                "4. Frames to AVOID with explanation",
                string.Empty,
                "## ANALYSIS INSTRUCTIONS:",
                string.Empty,
                "Step 1 - Face Shape Detection:",
                "Identify the face shape from: Oval, Round, Square, Heart, Diamond, Oblong/Rectangle, Triangle",
                string.Empty,
                "Step 2 - Facial Feature Analysis:",
                "- Face width vs height ratio",
                "- Jawline type: soft/angular/wide/narrow",
                "- Forehead width: wide/medium/narrow",
                "- Cheekbone prominence: high/medium/low",
                "- Eye spacing: close-set/average/wide-set",
                "- Nose bridge height: high/medium/low/flat",
                string.Empty,
                "Step 3 - Frame Recommendations:",
                "For each recommendation provide specific frame styles that complement the detected features.",
                string.Empty,
                "## CATALOG GROUNDING RULES:",
                "- Treat frameShape, fitType, styleTags, and frameMaterial from each candidate product as the catalog source of truth.",
                "- Use the image only for face analysis. Use the candidate product catalog for all product-specific recommendation details.",
                "- For each recommendation, the selected product's frameShape must drive frame_shape, fitType must drive frame_width, frameMaterial must drive material_suggestion, and styleTags must be referenced in why_suits_you whenever they help explain the fit.",
                "- Convert fitType to frame_width conservatively: narrow/slim => narrow, regular/medium/standard => medium, wide/oversize => wide, otherwise unknown.",
                "- Do not invent frame attributes that are not present in the selected candidate product.",
                "- If the catalog lacks a field, use 'unknown' rather than filling it with generic eyewear advice.",
                "- Rank higher the products whose frameShape, fitType, styleTags, and material are most consistent with the detected facial features and style personality.",
                string.Empty,
                "IMPORTANT RULES:",
                "- Always respond in pure JSON format only",
                "- All explanatory text (why_suits_you, reason, tips, summary) should be in Vietnamese",
                "- Be specific and detailed in recommendations",
                "- Base all recommendations on actual visible facial features",
                "- Match scores should reflect genuine suitability (top pick typically 85-95)",
                "- If face is not clearly visible, set confidence_score below 0.5 and note limitations in summary",
                "- If any field cannot be determined reliably from the image or product data, use 'unknown' or an empty array instead of guessing.",
                "- Only recommend from the candidate products below and include the exact productId for each recommendation.",
                string.Empty,
                "Candidate frame products (catalog data):",
                string.Join('\n', productLines)
            });
        }

        private static string FormatStyleTags(IEnumerable<string>? styleTags)
        {
            var tags = styleTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToList();

            return tags is { Count: > 0 }
                ? string.Join(", ", tags)
                : "unknown";
        }

        private static object BuildResponseSchema(int maxRecommendations)
        {
            return new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "analysis", "recommendations", "frames_to_avoid", "styling_tips", "summary" },
                properties = new
                {
                    analysis = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "face_shape", "confidence_score", "facial_features", "skin_tone", "style_personality" },
                        properties = new
                        {
                            face_shape = new { type = "string" },
                            confidence_score = new { type = "number", minimum = 0, maximum = 1 },
                            facial_features = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "face_ratio", "jawline", "forehead", "cheekbones", "eye_spacing", "nose_bridge" },
                                properties = new
                                {
                                    face_ratio = new { type = "string" },
                                    jawline = new { type = "string" },
                                    forehead = new { type = "string" },
                                    cheekbones = new { type = "string" },
                                    eye_spacing = new { type = "string" },
                                    nose_bridge = new { type = "string" }
                                }
                            },
                            skin_tone = new { type = "string" },
                            style_personality = new { type = "string" }
                        }
                    },
                    recommendations = new
                    {
                        type = "array",
                        maxItems = maxRecommendations,
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[]
                            {
                                "productId", "rank", "frame_style", "frame_shape", "why_suits_you", "frame_width",
                                "material_suggestion", "color_recommendations",
                                "size_guide", "brands_examples", "price_range", "occasions", "match_score"
                            },
                            properties = new
                            {
                                productId = new { type = "string" },
                                rank = new { type = "integer", minimum = 1, maximum = maxRecommendations },
                                frame_style = new { type = "string" },
                                frame_shape = new { type = "string" },
                                why_suits_you = new { type = "string" },
                                frame_width = new { type = "string" },
                                material_suggestion = new { type = "string" },
                                color_recommendations = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        required = new[] { "color", "hex", "reason" },
                                        properties = new
                                        {
                                            color = new { type = "string" },
                                            hex = new { type = "string" },
                                            reason = new { type = "string" }
                                        }
                                    }
                                },
                                size_guide = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "lens_width_mm", "bridge_mm", "temple_length_mm" },
                                    properties = new
                                    {
                                        lens_width_mm = new { type = "string" },
                                        bridge_mm = new { type = "string" },
                                        temple_length_mm = new { type = "string" }
                                    }
                                },
                                brands_examples = new
                                {
                                    type = "array",
                                    items = new { type = "string" }
                                },
                                price_range = new { type = "string" },
                                occasions = new
                                {
                                    type = "array",
                                    items = new { type = "string" }
                                },
                                match_score = new { type = "integer", minimum = 0, maximum = 100 }
                            }
                        }
                    },
                    frames_to_avoid = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "frame_style", "reason" },
                            properties = new
                            {
                                frame_style = new { type = "string" },
                                reason = new { type = "string" }
                            }
                        }
                    },
                    styling_tips = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    },
                    summary = new { type = "string" }
                }
            };
        }

        private static RecommendResponse MapToResponse(
            GeminiPayload payload,
            IReadOnlyList<Product> candidateProducts,
            int maxRecommendations)
        {
            var productLookup = candidateProducts
                .Where(product => !string.IsNullOrWhiteSpace(product.Id))
                .GroupBy(product => product.Id)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var recommendations = (payload.Recommendations ?? new List<GeminiItem>())
                .Where(item => !string.IsNullOrWhiteSpace(item.ProductId) && productLookup.ContainsKey(item.ProductId))
                .GroupBy(item => item.ProductId, StringComparer.Ordinal)
                .Select(group => group.OrderBy(item => item.Rank <= 0 ? int.MaxValue : item.Rank).First())
                .OrderBy(item => item.Rank <= 0 ? int.MaxValue : item.Rank)
                .ThenByDescending(item => item.MatchScore)
                .Take(maxRecommendations)
                .Select((item, index) =>
                {
                    var product = productLookup[item.ProductId];
                    return new RecommendItem
                    {
                        Rank = item.Rank > 0 ? item.Rank : index + 1,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductType = product.ProductType,
                        Price = product.Price,
                        ImageUrls = ProductImageUrlResolver.GetImageUrls(product),
                        FrameStyle = NormalizeUnknown(item.FrameStyle),
                        FrameShape = NormalizeUnknown(item.FrameShape, product.FrameDetails?.FrameShape),
                        WhySuitsYou = NormalizeNarrative(item.WhySuitsYou),
                        FrameWidth = NormalizeFrameWidth(item.FrameWidth, product.FrameDetails?.FitType),
                        MaterialSuggestion = NormalizeUnknown(item.MaterialSuggestion, product.FrameDetails?.FrameMaterial),
                        ColorRecommendations = NormalizeColorRecommendations(item.ColorRecommendations),
                        SizeGuide = NormalizeSizeGuide(item.SizeGuide),
                        BrandsExamples = NormalizeStringList(item.BrandsExamples),
                        PriceRange = NormalizeUnknown(item.PriceRange),
                        Occasions = NormalizeStringList(item.Occasions),
                        MatchScore = Math.Clamp(item.MatchScore, 0, 100)
                    };
                })
                .ToList();

            return new RecommendResponse
            {
                Analysis = NormalizeAnalysis(payload.Analysis),
                Recommendations = recommendations,
                FramesToAvoid = NormalizeFramesToAvoid(payload.FramesToAvoid),
                StylingTips = NormalizeStringList(payload.StylingTips),
                Summary = NormalizeNarrative(payload.Summary, "Khong du thong tin de dua ra tong ket chi tiet.")
            };
        }

        private static FaceAnalysis NormalizeAnalysis(FaceAnalysis? analysis)
        {
            return new FaceAnalysis
            {
                FaceShape = NormalizeUnknown(analysis?.FaceShape),
                ConfidenceScore = ClampConfidence(analysis?.ConfidenceScore ?? 0m),
                SkinTone = NormalizeUnknown(analysis?.SkinTone),
                StylePersonality = NormalizeUnknown(analysis?.StylePersonality),
                FacialFeatures = new FaceFeatures
                {
                    FaceRatio = NormalizeUnknown(analysis?.FacialFeatures?.FaceRatio),
                    Jawline = NormalizeUnknown(analysis?.FacialFeatures?.Jawline),
                    Forehead = NormalizeUnknown(analysis?.FacialFeatures?.Forehead),
                    Cheekbones = NormalizeUnknown(analysis?.FacialFeatures?.Cheekbones),
                    EyeSpacing = NormalizeUnknown(analysis?.FacialFeatures?.EyeSpacing),
                    NoseBridge = NormalizeUnknown(analysis?.FacialFeatures?.NoseBridge)
                }
            };
        }

        private static List<ColorTip> NormalizeColorRecommendations(IEnumerable<ColorTip>? items)
        {
            return items?
                .Where(item => !string.IsNullOrWhiteSpace(item.Color))
                .Select(item => new ColorTip
                {
                    Color = item.Color.Trim(),
                    Hex = NormalizeUnknown(item.Hex),
                    Reason = NormalizeNarrative(item.Reason)
                })
                .ToList() ?? new List<ColorTip>();
        }

        private static FrameSizeGuide NormalizeSizeGuide(FrameSizeGuide? sizeGuide)
        {
            return new FrameSizeGuide
            {
                LensWidthMm = NormalizeUnknown(sizeGuide?.LensWidthMm),
                BridgeMm = NormalizeUnknown(sizeGuide?.BridgeMm),
                TempleLengthMm = NormalizeUnknown(sizeGuide?.TempleLengthMm)
            };
        }

        private static List<AvoidFrameItem> NormalizeFramesToAvoid(IEnumerable<AvoidFrameItem>? items)
        {
            return items?
                .Where(item => !string.IsNullOrWhiteSpace(item.FrameStyle))
                .Select(item => new AvoidFrameItem
                {
                    FrameStyle = item.FrameStyle.Trim(),
                    Reason = NormalizeNarrative(item.Reason)
                })
                .ToList() ?? new List<AvoidFrameItem>();
        }

        private static List<string> NormalizeStringList(IEnumerable<string>? items)
        {
            return items?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList() ?? new List<string>();
        }

        private static string NormalizeFrameWidth(string? frameWidth, string? fitType)
        {
            if (!string.IsNullOrWhiteSpace(frameWidth))
                return frameWidth.Trim();

            if (string.Equals(fitType, "regular", StringComparison.OrdinalIgnoreCase))
                return "medium";

            return NormalizeUnknown(fitType);
        }

        private static string NormalizeUnknown(string? value, string? fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback.Trim();

            return "unknown";
        }

        private static string NormalizeNarrative(string? value, string fallback = "Khong du thong tin de giai thich chi tiet.")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static decimal ClampConfidence(decimal value)
        {
            if (value < 0)
                return 0;

            if (value > 1)
                return 1;

            return value;
        }

        private static bool IsFrameProduct(Product product)
        {
            return string.Equals(product.ProductType, "Frame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product.ProductType, "Glasses", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class GeminiGenerateContentRequest
        {
            [JsonPropertyName("system_instruction")]
            public GeminiContentRequest? SystemInstruction { get; set; }

            [JsonPropertyName("contents")]
            public List<GeminiContentRequest> Contents { get; set; } = new();

            [JsonPropertyName("generation_config")]
            public GeminiGenerationConfigRequest? GenerationConfig { get; set; }
        }

        private sealed class GeminiContentRequest
        {
            [JsonPropertyName("parts")]
            public List<GeminiPartRequest> Parts { get; set; } = new();
        }

        private sealed class GeminiPartRequest
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("inline_data")]
            public GeminiInlineDataRequest? InlineData { get; set; }
        }

        private sealed class GeminiInlineDataRequest
        {
            [JsonPropertyName("mime_type")]
            public string MimeType { get; set; } = string.Empty;

            [JsonPropertyName("data")]
            public string Data { get; set; } = string.Empty;
        }

        private sealed class GeminiGenerationConfigRequest
        {
            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("response_mime_type")]
            public string ResponseMimeType { get; set; } = "application/json";

            [JsonPropertyName("response_json_schema")]
            public object? ResponseJsonSchema { get; set; }
        }

        private sealed class GeminiGenerateContentResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidateResponse>? Candidates { get; set; }
        }

        private sealed class GeminiCandidateResponse
        {
            [JsonPropertyName("content")]
            public GeminiContentResponse? Content { get; set; }
        }

        private sealed class GeminiContentResponse
        {
            [JsonPropertyName("parts")]
            public List<GeminiPartResponse>? Parts { get; set; }
        }

        private sealed class GeminiPartResponse
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private sealed class GeminiPayload
        {
            [JsonPropertyName("analysis")]
            public FaceAnalysis? Analysis { get; set; }

            [JsonPropertyName("recommendations")]
            public List<GeminiItem>? Recommendations { get; set; }

            [JsonPropertyName("frames_to_avoid")]
            public List<AvoidFrameItem>? FramesToAvoid { get; set; }

            [JsonPropertyName("styling_tips")]
            public List<string>? StylingTips { get; set; }

            [JsonPropertyName("summary")]
            public string? Summary { get; set; }
        }

        private sealed class GeminiItem
        {
            [JsonPropertyName("productId")]
            public string ProductId { get; set; } = string.Empty;

            [JsonPropertyName("rank")]
            public int Rank { get; set; }

            [JsonPropertyName("frame_style")]
            public string? FrameStyle { get; set; }

            [JsonPropertyName("frame_shape")]
            public string? FrameShape { get; set; }

            [JsonPropertyName("why_suits_you")]
            public string? WhySuitsYou { get; set; }

            [JsonPropertyName("frame_width")]
            public string? FrameWidth { get; set; }

            [JsonPropertyName("material_suggestion")]
            public string? MaterialSuggestion { get; set; }

            [JsonPropertyName("color_recommendations")]
            public List<ColorTip>? ColorRecommendations { get; set; }

            [JsonPropertyName("size_guide")]
            public FrameSizeGuide? SizeGuide { get; set; }

            [JsonPropertyName("brands_examples")]
            public List<string>? BrandsExamples { get; set; }

            [JsonPropertyName("price_range")]
            public string? PriceRange { get; set; }

            [JsonPropertyName("occasions")]
            public List<string>? Occasions { get; set; }

            [JsonPropertyName("match_score")]
            public int MatchScore { get; set; }
        }
    }
}



