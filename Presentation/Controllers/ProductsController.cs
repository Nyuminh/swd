using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Product;
using swd.Application.Services;

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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(new { total = products.Count, data = products });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(string categoryId)
        {
            var products = await _productService.GetByCategoryAsync(categoryId);
            return Ok(new { total = products.Count, data = products });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(
            [FromForm] string name,
            [FromForm] string categoryId,
            [FromForm] string productType,
            [FromForm] decimal price,
            [FromForm] string size,
            [FromForm] string color,
            [FromForm] string targetGender,
            [FromForm] int inventoryQuantity,
            [FromForm] List<string>? imageUrls,
            [FromForm] string? frameShape,
            [FromForm] string? fitType,
            [FromForm] List<string>? styleTags,
            [FromForm] string? frameMaterial,
            [FromForm] string? lensType,
            [FromForm] string? lensIndex,
            [FromForm] List<string>? lensCoatings,
            [FromForm] int warrantyMonths)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var request = new CreateProductRequest
            {
                Name = name,
                CategoryId = categoryId,
                ProductType = productType,
                Price = price,
                Size = size,
                Color = color,
                TargetGender = targetGender,
                InventoryQuantity = inventoryQuantity,
                ImageUrls = imageUrls,
                WarrantyMonths = warrantyMonths,
                FrameDetails = BuildFrameDetailsRequest(frameShape, fitType, styleTags, frameMaterial),
                LensDetails = BuildLensDetailsRequest(lensType, lensIndex, lensCoatings)
            };

            var product = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(
            string id,
            [FromForm] string name,
            [FromForm] string categoryId,
            [FromForm] string productType,
            [FromForm] decimal price,
            [FromForm] string size,
            [FromForm] string color,
            [FromForm] string targetGender,
            [FromForm] int inventoryQuantity,
            [FromForm] List<string>? imageUrls,
            [FromForm] string? frameShape,
            [FromForm] string? fitType,
            [FromForm] List<string>? styleTags,
            [FromForm] string? frameMaterial,
            [FromForm] string? lensType,
            [FromForm] string? lensIndex,
            [FromForm] List<string>? lensCoatings,
            [FromForm] int warrantyMonths)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var request = new UpdateProductRequest
                {
                    Name = name,
                    CategoryId = categoryId,
                    ProductType = productType,
                    Price = price,
                    Size = size,
                    Color = color,
                    TargetGender = targetGender,
                    InventoryQuantity = inventoryQuantity,
                    ImageUrls = imageUrls,
                    WarrantyMonths = warrantyMonths,
                    FrameDetails = BuildFrameDetailsRequest(frameShape, fitType, styleTags, frameMaterial),
                    LensDetails = BuildLensDetailsRequest(lensType, lensIndex, lensCoatings)
                };

                var product = await _productService.UpdateAsync(id, request);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private static FrameDetailsRequest? BuildFrameDetailsRequest(
            string? frameShape,
            string? fitType,
            List<string>? styleTags,
            string? frameMaterial)
        {
            var normalizedStyleTags = styleTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToList();

            if (string.IsNullOrWhiteSpace(frameShape)
                && string.IsNullOrWhiteSpace(fitType)
                && string.IsNullOrWhiteSpace(frameMaterial)
                && (normalizedStyleTags is null || normalizedStyleTags.Count == 0))
            {
                return null;
            }

            return new FrameDetailsRequest
            {
                FrameShape = frameShape,
                FitType = fitType,
                StyleTags = normalizedStyleTags,
                FrameMaterial = frameMaterial
            };
        }

        private static LensDetailsRequest? BuildLensDetailsRequest(
            string? lensType,
            string? lensIndex,
            List<string>? lensCoatings)
        {
            var normalizedCoatings = lensCoatings?
                .Where(coating => !string.IsNullOrWhiteSpace(coating))
                .Select(coating => coating.Trim())
                .ToList();

            if (string.IsNullOrWhiteSpace(lensType)
                && string.IsNullOrWhiteSpace(lensIndex)
                && (normalizedCoatings is null || normalizedCoatings.Count == 0))
            {
                return null;
            }

            return new LensDetailsRequest
            {
                LensType = lensType,
                Index = lensIndex,
                Coatings = normalizedCoatings
            };
        }
    }
}
