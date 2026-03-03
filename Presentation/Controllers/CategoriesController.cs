using Microsoft.AspNetCore.Mvc;
using swd.Application.Services;
using swd.Application.DTOs.Category;
using System.Threading.Tasks;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _categoryService.CreateCategoryAsync(request);
            return Ok(category); // Using Ok for simplicity, can be changed to CreatedAtAction later
        }
    }
}