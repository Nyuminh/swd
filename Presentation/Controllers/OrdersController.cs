using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Order;
using swd.Application.Facades;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly CheckoutFacade _checkoutFacade;
        private readonly OrderService _orderService;

        public OrdersController(CheckoutFacade checkoutFacade, OrderService orderService)
        {
            _checkoutFacade = checkoutFacade;
            _orderService = orderService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(new { message = "Invalid token." });

            request.UserId = currentUserId;

            try
            {
                var response = await _checkoutFacade.PlaceOrder(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(new { total = orders.Count, data = orders });
        }

        [HttpGet("revenue")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var summary = await _orderService.GetRevenueSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (!CanAccessUserOrders(order.UserId))
                    return Forbid();

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId)
        {
            if (!CanAccessUserOrders(userId))
                return Forbid();

            var orders = await _orderService.GetOrdersByUserAsync(userId);
            return Ok(new { total = orders.Count, data = orders });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateOrder(string id, [FromBody] UpdateOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedOrder = await _orderService.UpdateOrderAsync(id, request);
                return Ok(updatedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteOrder(string id)
        {
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return Ok(new { message = "Order deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool CanAccessUserOrders(string? userId)
        {
            return IsPrivilegedRole()
                || string.Equals(GetCurrentUserId(), userId, StringComparison.Ordinal);
        }

        private bool IsPrivilegedRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}
