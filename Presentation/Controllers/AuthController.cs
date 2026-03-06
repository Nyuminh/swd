using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using swd.Application.DTOs.Auth;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    /// <summary>
    /// Authentication – Đăng ký, Đăng nhập, Xác minh email, Đổi/Reset mật khẩu
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Tags("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // ══════════════════════════════════════════════════════════
        // ĐĂNG KÝ / XÁC MINH EMAIL
        // ══════════════════════════════════════════════════════════

        /// <summary>Đăng ký tài khoản mới → gửi mã OTP 6 số qua Gmail</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = $"Đăng ký thành công nhưng không gửi được email: {ex.Message}" }); }
        }

        /// <summary>Xác minh email bằng mã OTP 6 số nhận qua Gmail</summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.VerifyEmailAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Gửi lại mã OTP xác minh email (khi mã cũ đã hết hạn)</summary>
        [HttpPost("resend-verification-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationCode([FromBody] ResendCodeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.ResendVerificationCodeAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = $"Không gửi được email: {ex.Message}" }); }
        }

        // ══════════════════════════════════════════════════════════
        // ĐĂNG NHẬP / PROFILE
        // ══════════════════════════════════════════════════════════

        /// <summary>Đăng nhập → trả về JWT token (chỉ khi email đã xác minh)</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return StatusCode(403, new { message = ex.Message }); }
        }

        /// <summary>Lấy thông tin tài khoản đang đăng nhập (cần JWT token)</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var profile = await _authService.GetProfileAsync(userId);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════
        // QUÊN MẬT KHẨU (không cần đăng nhập)
        // ══════════════════════════════════════════════════════════

        /// <summary>Quên mật khẩu → gửi OTP đặt lại mật khẩu qua Gmail</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.ForgotPasswordAsync(request);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = $"Không gửi được email: {ex.Message}" }); }
        }

        /// <summary>Đặt lại mật khẩu mới bằng OTP nhận từ email (không cần đăng nhập)</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════
        // ĐỔI MẬT KHẨU (cần đăng nhập)
        // ══════════════════════════════════════════════════════════

        /// <summary>Bước 1 – Gửi OTP đổi mật khẩu đến email đang đăng nhập</summary>
        [HttpPost("send-change-password-otp")]
        [Authorize]
        public async Task<IActionResult> SendChangePasswordOtp()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _authService.RequestChangePasswordOtpAsync(userId);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = $"Không gửi được OTP: {ex.Message}" }); }
        }

        /// <summary>Bước 2 – Xác nhận đổi mật khẩu: nhập OTP + mật khẩu cũ + mật khẩu mới</summary>
        [HttpPost("confirm-change-password")]
        [Authorize]
        public async Task<IActionResult> ConfirmChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _authService.ChangePasswordAsync(userId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
