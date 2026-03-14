using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.Promotion
{
    public class CreatePromotionRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Priority { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdatePromotionRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Priority { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
