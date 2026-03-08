using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.CartCombo;
using swd.Application.Exceptions;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/cart-combos")]
    [Authorize]
    public class CartCombosController : ControllerBase
    {
        private readonly CartComboService _cartComboService;

        public CartCombosController(CartComboService cartComboService)
        {
            _cartComboService = cartComboService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var cart = await _cartComboService.GetCartByUserIdAsync(userId);
                return Ok(cart);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("me/combos")]
        public async Task<IActionResult> AddCombo([FromBody] CreateCartComboRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var cart = await _cartComboService.AddComboAsync(userId, request);
                return Ok(cart);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("me/combos/{comboId}")]
        public async Task<IActionResult> UpdateCombo(string comboId, [FromBody] UpdateCartComboRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var cart = await _cartComboService.UpdateComboAsync(userId, comboId, request);
                return Ok(cart);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("me/combos/{comboId}")]
        public async Task<IActionResult> RemoveCombo(string comboId, [FromQuery] int? expectedVersion = null)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var cart = await _cartComboService.RemoveComboAsync(userId, comboId, expectedVersion);
                return Ok(cart);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("me")]
        public async Task<IActionResult> ClearCart([FromQuery] int? expectedVersion = null)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                await _cartComboService.ClearCartAsync(userId, expectedVersion);
                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ConcurrencyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}
