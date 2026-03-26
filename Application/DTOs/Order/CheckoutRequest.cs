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
        public EyePrescriptionDto? LeftEye { get; set; }
        public EyePrescriptionDto? RightEye { get; set; }
        public decimal? PupillaryDistance { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class EyePrescriptionDto
    {
        public decimal? Sphere { get; set; }
        public decimal? Cylinder { get; set; }
        public int? Axis { get; set; }
        public decimal? Add { get; set; }
    }

    public class CheckoutRequest
    {
        public string? UserId { get; set; }

        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        public List<CheckoutItemRequest> Items { get; set; } = new List<CheckoutItemRequest>();

        [Required]
        public string ShippingOptionId { get; set; } = string.Empty;

        [Required]
        public string PaymentOptionId { get; set; } = string.Empty;

        [Required]
        public CheckoutShippingRequest Shipping { get; set; } = new();

        public string? PaymentMethod { get; set; }

        public CheckoutPrescriptionRequest? Prescription { get; set; }

        public string? PromotionId { get; set; }
    }
}
