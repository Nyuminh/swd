using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Settings;

namespace swd.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LogoutAsync_ShouldMarkPreviouslyIssuedTokenAsRevoked()
    {
        var user = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Username = "customer01",
            Email = "customer01@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Customer",
            IsEmailVerified = true
        };
        var userRepository = new InMemoryUserRepository(new List<User> { user });
        var jwtTokenService = CreateJwtTokenService();
        var tokenRevocationService = new TokenRevocationService(userRepository);
        var authService = new AuthService(userRepository, jwtTokenService, tokenRevocationService);

        var token = jwtTokenService.GenerateToken(user);
        var issuedAtUtc = ReadIssuedAtUtc(token);

        await authService.LogoutAsync(user.Id);

        Assert.NotNull(user.TokenInvalidBeforeUtc);
        Assert.True(await tokenRevocationService.IsTokenRevokedAsync(user.Id, issuedAtUtc));
    }

    [Fact]
    public async Task IsTokenRevokedAsync_ShouldReturnFalse_WhenTokenWasIssuedAfterInvalidationTime()
    {
        var user = new User
        {
            Id = "507f1f77bcf86cd799439012",
            Username = "customer02",
            Email = "customer02@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "Customer",
            IsEmailVerified = true,
            TokenInvalidBeforeUtc = new DateTime(2026, 3, 9, 10, 0, 0, DateTimeKind.Utc)
        };
        var userRepository = new InMemoryUserRepository(new List<User> { user });
        var tokenRevocationService = new TokenRevocationService(userRepository);

        var isRevoked = await tokenRevocationService.IsTokenRevokedAsync(
            user.Id,
            new DateTime(2026, 3, 9, 10, 0, 1, DateTimeKind.Utc));

        Assert.False(isRevoked);
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        return new JwtTokenService(Options.Create(new JwtSettings
        {
            SecretKey = "super-secret-key-with-at-least-32-chars",
            Issuer = "swd-tests",
            Audience = "swd-tests",
            ExpiryMinutes = 60
        }));
    }

    private static DateTime ReadIssuedAtUtc(string token)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var issuedAtClaim = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Iat).Value;
        var issuedAtSeconds = long.Parse(issuedAtClaim);
        return DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds).UtcDateTime;
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users;

        public InMemoryUserRepository(List<User>? seed = null)
        {
            _users = seed ?? new List<User>();
        }

        public Task<List<User>> GetAllAsync()
        {
            return Task.FromResult(_users.ToList());
        }

        public Task<User> GetByIdAsync(string id)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Id == id)!);
        }

        public Task CreateAsync(User entity)
        {
            _users.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, User entity)
        {
            var index = _users.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                _users[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _users.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Username == username));
        }

        public Task<bool> ExistsAsync(string email, string username)
        {
            var exists = _users.Any(x => x.Email == email || x.Username == username);
            return Task.FromResult(exists);
        }

        public Task UpdateVerificationAsync(string userId, string code, DateTime expiry)
        {
            throw new NotImplementedException();
        }

        public Task MarkEmailVerifiedAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePasswordResetCodeAsync(string userId, string code, DateTime expiry)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUserInfoAsync(string userId, string username, string email, string phone, string address)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUserRoleAsync(string userId, string newRole)
        {
            throw new NotImplementedException();
        }

        public Task SetTokenInvalidBeforeAsync(string userId, DateTime invalidBeforeUtc)
        {
            var user = _users.First(x => x.Id == userId);
            user.TokenInvalidBeforeUtc = invalidBeforeUtc;
            return Task.CompletedTask;
        }
    }
}
