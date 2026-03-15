using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.Order
{
    public class CreateReturnRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class ProcessReturnRequest
    {
        [Required]
        public string Outcome { get; set; } = string.Empty;
    }
}
