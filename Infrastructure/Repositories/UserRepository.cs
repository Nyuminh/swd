using MongoDB.Driver;
using swd.Infrastructure.Persistence;
using swd.Domain.Interfaces;

namespace swd.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(MongoDbContext context) : base(context)
        {
            _users = context.Database.GetCollection<User>("Users");
        }

        // ── Overrides: fix ObjectId conversion issue từ base Repository ──
        // Base dùng Builders<T>.Filter.Eq("_id", id) với string thô →
        // MongoDB không match được ObjectId → không tìm/xóa được.
        // Typed expression (u => u.Id == id) thì MongoDB driver tự xử lý đúng.

        public new async Task<User?> GetByIdAsync(string id)
        {
            return await _users
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();
        }

        public new async Task DeleteAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public new async Task UpdateAsync(string id, User entity)
        {
            await _users.ReplaceOneAsync(u => u.Id == id, entity);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _users
                .Find(u => u.Email.ToLower() == email.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _users
                .Find(u => u.Username.ToLower() == username.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(string email, string username)
        {
            var count = await _users
                .Find(u => u.Email.ToLower() == email.ToLower()
                        || u.Username.ToLower() == username.ToLower())
                .CountDocumentsAsync();
            return count > 0;
        }

        public async Task UpdateVerificationAsync(string userId, string code, DateTime expiry)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.VerificationCode, code)
                .Set(u => u.VerificationCodeExpiry, expiry);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task MarkEmailVerifiedAsync(string userId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.IsEmailVerified, true)
                .Unset(u => u.VerificationCode)
                .Unset(u => u.VerificationCodeExpiry);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task UpdatePasswordResetCodeAsync(string userId, string code, DateTime expiry)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.PasswordResetCode, code)
                .Set(u => u.PasswordResetCodeExpiry, expiry);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, newPasswordHash)
                .Unset(u => u.PasswordResetCode)
                .Unset(u => u.PasswordResetCodeExpiry);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task UpdateUserInfoAsync(string userId, string username, string email, string phone, string address)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.Username, username)
                .Set(u => u.Email, email)
                .Set(u => u.Phone, phone)
                .Set(u => u.Address, address);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task UpdateUserRoleAsync(string userId, string newRole)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.Role, newRole);
            await _users.UpdateOneAsync(filter, update);
        }
    }
}

