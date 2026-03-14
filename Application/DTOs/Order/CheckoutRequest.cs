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

    public class CheckoutShippingRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        public string Phone { get; set; } = string.Empty;
        public string Method { get; set; } = "Standard";
    }

    public class CheckoutPrescriptionRequest
    {
        public string? LeftEye { get; set; }
        public string? RightEye { get; set; }
        public string? Describe { get; set; }
    }

    public class CheckoutRequest
    {
        public string? UserId { get; set; }

        [Required]
        public List<CheckoutItemRequest> Items { get; set; } = new List<CheckoutItemRequest>();

        [Required]
        public CheckoutShippingRequest Shipping { get; set; } = new();

        [Required]
        public string PaymentMethod { get; set; } = "COD";

        public CheckoutPrescriptionRequest? Prescription { get; set; }

        public string? PromotionId { get; set; }
    }
}
