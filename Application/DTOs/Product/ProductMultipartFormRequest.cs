using Microsoft.AspNetCore.Http;

namespace swd.Application.DTOs.Product
{
    public class ProductMultipartFormRequest
    {
        public string Name { get; set; } = string.Empty;

        public string CategoryId { get; set; } = string.Empty;

        public string ProductType { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Size { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string TargetGender { get; set; } = string.Empty;

        public int InventoryQuantity { get; set; }

        public List<IFormFile>? ImageFiles { get; set; }

        public string? FrameShape { get; set; }

        public string? FitType { get; set; }

        public List<string>? StyleTags { get; set; }

        public string? FrameMaterial { get; set; }

        public string? LensType { get; set; }

        public string? LensIndex { get; set; }

        public List<string>? LensCoatings { get; set; }

        public int WarrantyMonths { get; set; }
    }
}
