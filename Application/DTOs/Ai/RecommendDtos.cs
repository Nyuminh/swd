using System.Text.Json.Serialization;

namespace swd.Application.DTOs.Ai
{
    public class RecommendRequest
    {
        public IFormFile? Portrait { get; set; }

        public int? MaxRecommendations { get; set; }
    }

    public class RecommendResponse
    {
        [JsonPropertyName("analysis")]
        public FaceAnalysis Analysis { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<RecommendItem> Recommendations { get; set; } = new();

        [JsonPropertyName("frames_to_avoid")]
        public List<AvoidFrameItem> FramesToAvoid { get; set; } = new();

        [JsonPropertyName("styling_tips")]
        public List<string> StylingTips { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }

    public class FaceAnalysis
    {
        [JsonPropertyName("face_shape")]
        public string FaceShape { get; set; } = "unknown";

        [JsonPropertyName("confidence_score")]
        public decimal ConfidenceScore { get; set; }

        [JsonPropertyName("facial_features")]
        public FaceFeatures FacialFeatures { get; set; } = new();

        [JsonPropertyName("skin_tone")]
        public string SkinTone { get; set; } = "unknown";

        [JsonPropertyName("style_personality")]
        public string StylePersonality { get; set; } = "unknown";
    }

    public class FaceFeatures
    {
        [JsonPropertyName("face_ratio")]
        public string FaceRatio { get; set; } = "unknown";

        [JsonPropertyName("jawline")]
        public string Jawline { get; set; } = "unknown";

        [JsonPropertyName("forehead")]
        public string Forehead { get; set; } = "unknown";

        [JsonPropertyName("cheekbones")]
        public string Cheekbones { get; set; } = "unknown";

        [JsonPropertyName("eye_spacing")]
        public string EyeSpacing { get; set; } = "unknown";

        [JsonPropertyName("nose_bridge")]
        public string NoseBridge { get; set; } = "unknown";
    }

    public class RecommendItem
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("product_type")]
        public string ProductType { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("image_urls")]
        public List<string> ImageUrls { get; set; } = new();

        [JsonPropertyName("frame_style")]
        public string FrameStyle { get; set; } = string.Empty;

        [JsonPropertyName("frame_shape")]
        public string FrameShape { get; set; } = string.Empty;

        [JsonPropertyName("why_suits_you")]
        public string WhySuitsYou { get; set; } = string.Empty;

        [JsonPropertyName("frame_width")]
        public string FrameWidth { get; set; } = "unknown";

        [JsonPropertyName("material_suggestion")]
        public string MaterialSuggestion { get; set; } = string.Empty;

        [JsonPropertyName("color_recommendations")]
        public List<ColorTip> ColorRecommendations { get; set; } = new();

        [JsonPropertyName("size_guide")]
        public FrameSizeGuide SizeGuide { get; set; } = new();

        [JsonPropertyName("brands_examples")]
        public List<string> BrandsExamples { get; set; } = new();

        [JsonPropertyName("price_range")]
        public string PriceRange { get; set; } = string.Empty;

        [JsonPropertyName("occasions")]
        public List<string> Occasions { get; set; } = new();

        [JsonPropertyName("match_score")]
        public int MatchScore { get; set; }
    }

    public class ColorTip
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;

        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    public class FrameSizeGuide
    {
        [JsonPropertyName("lens_width_mm")]
        public string LensWidthMm { get; set; } = "unknown";

        [JsonPropertyName("bridge_mm")]
        public string BridgeMm { get; set; } = "unknown";

        [JsonPropertyName("temple_length_mm")]
        public string TempleLengthMm { get; set; } = "unknown";
    }

    public class AvoidFrameItem
    {
        [JsonPropertyName("frame_style")]
        public string FrameStyle { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}

