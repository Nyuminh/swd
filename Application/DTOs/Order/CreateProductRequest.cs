namespace swd.Application.DTOs.Product
{
    public class CreateProductRequest
    {
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public string ProductType { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public string TargetGender { get; set; }
        public int InventoryQuantity { get; set; }
        public List<string>? ImageUrls { get; set; }
        public int WarrantyMonths { get; set; }
        public FrameDetailsRequest? FrameDetails { get; set; }
        public LensDetailsRequest? LensDetails { get; set; }
    }

    public class FrameDetailsRequest
    {
        public string? FrameShape { get; set; }
        public string? FitType { get; set; }
        public List<string>? StyleTags { get; set; }
        public string? FrameMaterial { get; set; }
    }

    public class LensDetailsRequest
    {
        public string? LensType { get; set; }
        public string? Index { get; set; }
        public List<string>? Coatings { get; set; }
    }
}
