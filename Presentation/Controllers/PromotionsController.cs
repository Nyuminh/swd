using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Promotion;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController : ControllerBase
    {
        private readonly PromotionService _promotionService;

        public PromotionsController(PromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var promotions = await _promotionService.GetAllAsync();
            return Ok(new { total = promotions.Count, data = promotions });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var promotion = await _promotionService.GetByIdAsync(id);
                return Ok(promotion);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] CreatePromotionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var promotion = await _promotionService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = promotion.Id }, promotion);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePromotionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var promotion = await _promotionService.UpdateAsync(id, request);
                return Ok(promotion);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
                await _promotionService.DeleteAsync(id);
                return Ok(new { message = "Promotion deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
