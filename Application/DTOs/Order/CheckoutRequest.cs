using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.Order
{
    public class CheckoutItemRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }
    }

    public class CheckoutRequest
    {
        public string? UserId { get; set; }

        [Required]
        public List<CheckoutItemRequest> Items { get; set; } = new List<CheckoutItemRequest>();

        public string? PromotionId { get; set; }
    }
}
