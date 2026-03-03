using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Order;
using swd.Application.Facades;
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutFacade _facade;

    public OrdersController(CheckoutFacade facade)
    {
        _facade = facade;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var response = await _facade.PlaceOrder(request);
        return Ok(response);
    }
}