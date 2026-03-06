using swd.Domain.Interfaces;

namespace swd.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> ExistsAsync(string email, string username);

        /// <summary>Lưu mã xác minh và thời hạn vào user</summary>
        Task UpdateVerificationAsync(string userId, string code, DateTime expiry);

        /// <summary>Đánh dấu email đã xác minh, xóa code</summary>
        Task MarkEmailVerifiedAsync(string userId);

        /// <summary>Lưu mã OTP reset mật khẩu</summary>
        Task UpdatePasswordResetCodeAsync(string userId, string code, DateTime expiry);

        /// <summary>Cập nhật mật khẩu mới và xóa reset code</summary>
        Task UpdatePasswordAsync(string userId, string newPasswordHash);

        /// <summary>Cập nhật thông tin cơ bản của user (Admin/Staff)</summary>
        Task UpdateUserInfoAsync(string userId, string username, string email, string phone, string address);

        /// <summary>Đổi role của user (chỉ Admin)</summary>
        Task UpdateUserRoleAsync(string userId, string newRole);
    }
}

