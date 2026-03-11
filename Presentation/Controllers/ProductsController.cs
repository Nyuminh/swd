using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ProductService productService, IWebHostEnvironment webHostEnvironment)
        {
            _productService = productService;
            _webHostEnvironment = webHostEnvironment;
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
        [Authorize(Roles = "Admin,Staff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProductMultipartFormRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var imageValidationMessage = ValidateImageFiles(request.ImageFiles);
            if (imageValidationMessage is not null)
                return BadRequest(new { message = imageValidationMessage });

            var imageUrls = await SaveImageFilesAsync(request.ImageFiles, HttpContext.RequestAborted);
            var createRequest = new CreateProductRequest
            {
                Name = request.Name,
                CategoryId = request.CategoryId,
                ProductType = request.ProductType,
                Price = request.Price,
                Size = request.Size,
                Color = request.Color,
                TargetGender = request.TargetGender,
                InventoryQuantity = request.InventoryQuantity,
                ImageUrls = imageUrls,
                WarrantyMonths = request.WarrantyMonths,
                FrameDetails = BuildFrameDetailsRequest(request.FrameShape, request.FitType, request.StyleTags, request.FrameMaterial),
                LensDetails = BuildLensDetailsRequest(request.LensType, request.LensIndex, request.LensCoatings)
            };

            var product = await _productService.CreateAsync(createRequest);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string id, [FromForm] ProductMultipartFormRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var imageValidationMessage = ValidateImageFiles(request.ImageFiles);
            if (imageValidationMessage is not null)
                return BadRequest(new { message = imageValidationMessage });

            try
            {
                var imageUrls = request.ImageFiles is null
                    ? null
                    : await SaveImageFilesAsync(request.ImageFiles, HttpContext.RequestAborted);

                var updateRequest = new UpdateProductRequest
                {
                    Name = request.Name,
                    CategoryId = request.CategoryId,
                    ProductType = request.ProductType,
                    Price = request.Price,
                    Size = request.Size,
                    Color = request.Color,
                    TargetGender = request.TargetGender,
                    InventoryQuantity = request.InventoryQuantity,
                    ImageUrls = imageUrls,
                    WarrantyMonths = request.WarrantyMonths,
                    FrameDetails = BuildFrameDetailsRequest(request.FrameShape, request.FitType, request.StyleTags, request.FrameMaterial),
                    LensDetails = BuildLensDetailsRequest(request.LensType, request.LensIndex, request.LensCoatings)
                };

                var product = await _productService.UpdateAsync(id, updateRequest);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
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

        private static string? ValidateImageFiles(IEnumerable<IFormFile>? imageFiles)
        {
            if (imageFiles is null)
                return null;

            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length == 0)
                    return "Product image is empty.";

                if (string.IsNullOrWhiteSpace(imageFile.ContentType)
                    || !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return "Product image must be an image.";
                }
            }

            return null;
        }

        private async Task<List<string>> SaveImageFilesAsync(IEnumerable<IFormFile>? imageFiles, CancellationToken cancellationToken)
        {
            var urls = new List<string>();
            if (imageFiles is null)
                return urls;

            var webRootPath = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
                ? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot")
                : _webHostEnvironment.WebRootPath;

            var uploadDirectory = Path.Combine(webRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadDirectory);

            foreach (var imageFile in imageFiles)
            {
                var extension = Path.GetExtension(imageFile.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = imageFile.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                        ? ".png"
                        : ".jpg";
                }

                var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var filePath = Path.Combine(uploadDirectory, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await imageFile.CopyToAsync(stream, cancellationToken);
                urls.Add($"/uploads/products/{fileName}");
            }

            return urls;
        }
    }
}
