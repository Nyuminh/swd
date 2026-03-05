using swd.Application.DTOs.User;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class UserManagementService
    {
        private readonly IUserRepository _userRepository;

        // Các role hợp lệ trong hệ thống
        private static readonly HashSet<string> ValidRoles = new() { "Admin", "Staff", "Customer" };

        // Staff chỉ được tạo/gán role Customer
        private static readonly HashSet<string> StaffAllowedRoles = new() { "Customer" };

        public UserManagementService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ─────────────────────────────────────────────────────────────
        // GET ALL USERS (Admin + Staff)
        // ─────────────────────────────────────────────────────────────
        public async Task<List<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToResponse).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // GET USER BY ID (Admin + Staff)
        // ─────────────────────────────────────────────────────────────
        public async Task<UserResponse> GetUserByIdAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy user với id: {id}");

            return MapToResponse(user);
        }

        // ─────────────────────────────────────────────────────────────
        // CREATE USER (Admin: mọi role | Staff: chỉ Customer)
        // ─────────────────────────────────────────────────────────────
        public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, string callerRole)
        {
            // Validate role hợp lệ
            if (!ValidRoles.Contains(request.Role))
                throw new InvalidOperationException($"Role '{request.Role}' không hợp lệ. Hợp lệ: Admin, Staff, Customer.");

            // Staff chỉ được tạo Customer
            if (callerRole == "Staff" && !StaffAllowedRoles.Contains(request.Role))
                throw new UnauthorizedAccessException("Staff chỉ được tạo tài khoản với role Customer.");

            // Kiểm tra trùng email/username
            var exists = await _userRepository.ExistsAsync(request.Email, request.Username);
            if (exists)
                throw new InvalidOperationException("Email hoặc Username đã được sử dụng.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Phone = request.Phone ?? string.Empty,
                Address = request.Address ?? string.Empty,
                Role = request.Role,
                IsEmailVerified = true, // Tạo bởi Admin/Staff → kích hoạt ngay
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);
            return MapToResponse(user);
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE USER INFO (Admin + Staff – chỉ thông tin cơ bản)
        // ─────────────────────────────────────────────────────────────
        public async Task<UserResponse> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy user với id: {id}");

            await _userRepository.UpdateUserInfoAsync(
                id,
                request.Username,
                request.Email,
                request.Phone,
                request.Address);

            // Trả về bản cập nhật
            user.Username = request.Username;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.Address = request.Address;

            return MapToResponse(user);
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE ROLE (chỉ Admin)
        // ─────────────────────────────────────────────────────────────
        public async Task<UserResponse> UpdateUserRoleAsync(string id, UpdateUserRoleRequest request)
        {
            if (!ValidRoles.Contains(request.Role))
                throw new InvalidOperationException($"Role '{request.Role}' không hợp lệ. Hợp lệ: Admin, Staff, Customer.");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy user với id: {id}");

            await _userRepository.UpdateUserRoleAsync(id, request.Role);
            user.Role = request.Role;

            return MapToResponse(user);
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE USER (chỉ Admin)
        // ─────────────────────────────────────────────────────────────
        public async Task DeleteUserAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"Không tìm thấy user với id: {id}");

            await _userRepository.DeleteAsync(id);
        }

        // ─────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────
        private static UserResponse MapToResponse(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address,
            Role = user.Role,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }
}
