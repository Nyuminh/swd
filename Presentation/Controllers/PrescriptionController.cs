using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Order;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/prescriptions")]
    [Authorize]
    public class PrescriptionController : ControllerBase
    {
        private readonly PrescriptionService _prescriptionService;

        public PrescriptionController(PrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        /// <summary>
        /// Verify (approve/reject) a prescription attached to an order.
        /// </summary>
        [HttpPut("orders/{orderId}/verify")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> VerifyPrescription(string orderId, [FromBody] VerifyPrescriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _prescriptionService.VerifyPrescriptionAsync(orderId, request);
                return Ok(response);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>
        /// Get the prescription details for a specific order.
        /// </summary>
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetPrescriptionByOrderId(string orderId)
        {
            try
            {
                var prescription = await _prescriptionService.GetPrescriptionByOrderIdAsync(orderId);
                if (prescription == null)
                    return NotFound(new { message = "No prescription found for this order." });

                return Ok(prescription);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>
        /// List all orders with prescriptions pending verification (Staff/Admin only).
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPendingPrescriptions()
        {
            var orders = await _prescriptionService.GetOrdersPendingVerificationAsync();
            return Ok(new { total = orders.Count, data = orders });
        }
    }
}
