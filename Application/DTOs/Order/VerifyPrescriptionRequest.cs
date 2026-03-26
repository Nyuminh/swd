using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.Order
{
    public class VerifyPrescriptionRequest
    {
        [Required]
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }
}
