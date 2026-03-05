using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using swd.Application.DTOs.User;
using swd.Application.Services;

namespace swd.Presentation.Controllers
{
    /// <summary>
    /// User Management – CRUD người dùng (Admin và Staff)
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Tags("Users")]
    [Authorize(Roles = "Admin,Staff")]
    public class UsersController : ControllerBase
    {
        private readonly UserManagementService _userService;

        public UsersController(UserManagementService userService)
        {
            _userService = userService;
        }

        // ══════════════════════════════════════════════════════════
        // READ
        // ══════════════════════════════════════════════════════════

        /// <summary>Lấy danh sách tất cả người dùng [Admin, Staff]</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(new { total = users.Count, data = users });
        }

        /// <summary>Lấy thông tin chi tiết một người dùng theo ID [Admin, Staff]</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════
        // CREATE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo người dùng mới – tài khoản kích hoạt ngay, không cần xác minh email
        /// [Admin: tạo được mọi role | Staff: chỉ tạo được Customer]
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";

            try
            {
                var user = await _userService.CreateUserAsync(request, callerRole);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════
        // UPDATE
        // ══════════════════════════════════════════════════════════

        /// <summary>Cập nhật thông tin người dùng (username, email, phone, address) [Admin, Staff]</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = await _userService.UpdateUserAsync(id, request);
                return Ok(user);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        /// <summary>
        /// Thay đổi role của người dùng [chỉ Admin]
        /// Role hợp lệ: Admin | Staff | Customer
        /// </summary>
        [HttpPatch("{id}/update-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = await _userService.UpdateUserRoleAsync(id, request);
                return Ok(new { message = $"Đã đổi role thành '{request.Role}' thành công.", user });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════
        // DELETE
        // ══════════════════════════════════════════════════════════

        /// <summary>Xóa người dùng khỏi hệ thống [chỉ Admin] (không thể xóa chính mình)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub")
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (callerId == id)
                return BadRequest(new { message = "Không thể xóa tài khoản của chính mình." });

            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { message = "Xóa người dùng thành công." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}
