using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Order;
using swd.Application.Facades;
using swd.Application.Services;
using System.Threading.Tasks;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly CheckoutFacade _checkoutFacade;
        private readonly OrderService _orderService;

        public OrdersController(CheckoutFacade checkoutFacade, OrderService orderService)
        {
            _checkoutFacade = checkoutFacade;
            _orderService = orderService;
        }

        // CREATE - Checkout (existing endpoint)
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _checkoutFacade.PlaceOrder(request);
                return Ok(response);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // READ - Get all orders
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(new { total = orders.Count, data = orders });
        }

        // READ - Get order by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // READ - Get orders by user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId)
        {
            var orders = await _orderService.GetOrdersByUserAsync(userId);
            return Ok(new { total = orders.Count, data = orders });
        }

        // UPDATE - Update order
        [HttpPut("{id}")]
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

        // DELETE - Delete order
        [HttpDelete("{id}")]
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
    }
}