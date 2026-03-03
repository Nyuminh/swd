using Microsoft.AspNetCore.Mvc;
using swd.Application.Services;
using swd.Application.DTOs.Product;
using System.Threading.Tasks;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        // Note: GetProductById is not implemented yet, but is required for CreatedAtAction.
        // You can add it later.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            // Implementation for getting a product by ID would go here.
            return Ok(new { Message = $"GetProductById with id {id} not implemented yet." });
        }
    }
}