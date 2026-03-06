using System.ComponentModel.DataAnnotations;

namespace swd.Application.DTOs.User
{
    // ── Responses ────────────────────────────────────────────────

    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Requests ─────────────────────────────────────────────────

    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        [MinLength(3, ErrorMessage = "Username phải có ít nhất 3 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Address { get; set; }

        /// <summary>Admin: Admin/Staff/Customer | Staff: chỉ Customer</summary>
        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = "Customer";
    }

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        [MinLength(3, ErrorMessage = "Username phải có ít nhất 3 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateUserRoleRequest
    {
        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = string.Empty;
    }
}
