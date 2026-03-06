using System.Collections.Generic;

namespace swd.Application.DTOs.Product
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string TargetGender { get; set; } = string.Empty;
        public int InventoryQuantity { get; set; }
        public List<string>? ImageUrls { get; set; }
        public int WarrantyMonths { get; set; }
    }
}
