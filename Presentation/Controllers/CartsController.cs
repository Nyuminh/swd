using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Cart;
using swd.Application.Exceptions;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/carts")]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartsController(CartService cartService)
        {
            _cartService = cartService;
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
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                return Ok(cart);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("me/items")]
        public async Task<IActionResult> AddItemToCart([FromBody] AddToCartRequest request)
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
                var cart = await _cartService.AddToCartAsync(userId, request);
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

        [HttpPut("me/items/{productId}")]
        public async Task<IActionResult> UpdateCartItem(string productId, [FromBody] UpdateCartItemRequest request)
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
                var cart = await _cartService.UpdateCartItemAsync(userId, productId, request);
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

        [HttpDelete("me/items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(string productId, [FromQuery] int? expectedVersion = null)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var cart = await _cartService.RemoveFromCartAsync(userId, productId, expectedVersion);
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
                await _cartService.ClearCartAsync(userId, expectedVersion);
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
