using System.Collections.Generic;

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
        public List<string> ImageUrls { get; set; }
        public int WarrantyMonths { get; set; }
    }
}