using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly GeminiRecommendationService _geminiRecommendationService;

        public AiController(GeminiRecommendationService geminiRecommendationService)
        {
            _geminiRecommendationService = geminiRecommendationService;
        }

        [HttpPost("glasses-recommendations")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RecommendGlasses(
            [FromForm] IFormFile portrait,
            [FromForm] int? maxRecommendations)
        {
            if (portrait is null || portrait.Length == 0)
                return BadRequest(new { message = "Portrait image is required." });

            if (string.IsNullOrWhiteSpace(portrait.ContentType) || !portrait.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Portrait file must be an image." });

            try
            {
                await using var memoryStream = new MemoryStream();
                await portrait.CopyToAsync(memoryStream, HttpContext.RequestAborted);

                var response = await _geminiRecommendationService.RecommendAsync(
                    memoryStream.ToArray(),
                    portrait.ContentType,
                    maxRecommendations,
                    HttpContext.RequestAborted);

                return Ok(response);
            }
            catch (InvalidOperationException ex) when (string.Equals(ex.Message, "Gemini API key is not configured.", StringComparison.Ordinal))
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                return StatusCode(502, new { message = $"Gemini API returned invalid JSON: {ex.Message}" });
            }
        }
    }
}
