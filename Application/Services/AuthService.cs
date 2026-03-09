using swd.Application.DTOs.Auth;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenService _jwtTokenService;
        private readonly TokenRevocationService _tokenRevocationService;

        public AuthService(
            IUserRepository userRepository,
            JwtTokenService jwtTokenService,
            TokenRevocationService tokenRevocationService)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _tokenRevocationService = tokenRevocationService;
        }

        public async Task<object> RegisterAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                throw new InvalidOperationException("Confirm password does not match.");

            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
                throw new InvalidOperationException("Username is already in use.");

            var user = new User
            {
                Username = request.Username,
                Email = string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone = request.Phone ?? string.Empty,
                Address = request.Address ?? string.Empty,
                Role = "Customer",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            return new
            {
                message = "Register successful.",
                username = user.Username
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
                throw new UnauthorizedAccessException("Username or password is incorrect.");

            var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isValidPassword)
                throw new UnauthorizedAccessException("Username or password is incorrect.");

            var token = _jwtTokenService.GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                ExpiresIn = _jwtTokenService.GetExpirySeconds(),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified
                }
            };
        }

        public async Task LogoutAsync(string userId)
        {
            await _tokenRevocationService.RevokeUserTokensAsync(userId);
        }

        public async Task<UserInfo> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsEmailVerified = user.IsEmailVerified
            };
        }

        public async Task<object> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new InvalidOperationException("Old password is incorrect.");

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new InvalidOperationException("Confirm new password does not match.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdatePasswordAsync(user.Id, newHash);

            return new { message = "Password changed successfully." };
        }
    }
}
