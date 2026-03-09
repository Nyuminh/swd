using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.Order
{
    public class CheckoutRequest
    {
        public string? UserId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        public string? PromotionId { get; set; }
    }
}
