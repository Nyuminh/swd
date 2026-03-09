namespace swd.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> ExistsAsync(string email, string username);
        Task UpdateVerificationAsync(string userId, string code, DateTime expiry);
        Task MarkEmailVerifiedAsync(string userId);
        Task UpdatePasswordResetCodeAsync(string userId, string code, DateTime expiry);
        Task UpdatePasswordAsync(string userId, string newPasswordHash);
        Task UpdateUserInfoAsync(string userId, string username, string email, string phone, string address);
        Task UpdateUserRoleAsync(string userId, string newRole);
        Task SetTokenInvalidBeforeAsync(string userId, DateTime invalidBeforeUtc);
    }
}
