using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.CartCombo
{
    public class CreateCartComboRequest
    {
        [Required]
        public string ComboId { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public string? PromotionId { get; set; }
        public bool UseBestPromotion { get; set; } = true;
        public int? ExpectedVersion { get; set; }
    }

    public class UpdateCartComboRequest
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public string? PromotionId { get; set; }
        public bool UseBestPromotion { get; set; } = true;
        public int? ExpectedVersion { get; set; }
    }

    public class CartComboItemResponse
    {
        public string ComboId { get; set; } = string.Empty;
        public string ComboName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? PromotionId { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal FinalUnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CartComboResponse
    {
        public string CartId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<CartComboItemResponse> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
