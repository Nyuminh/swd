using swd.Application.DTOs.Auth;
using swd.Domain.Interfaces;
using swd.Settings;
using Microsoft.Extensions.Options;

namespace swd.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenService _jwtTokenService;
        private readonly EmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public AuthService(
            IUserRepository userRepository,
            JwtTokenService jwtTokenService,
            EmailService emailService,
            IOptions<EmailSettings> emailSettings)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTER: tạo user → gửi mã xác minh qua Gmail
        // ─────────────────────────────────────────────────────────────
        public async Task<object> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra Email/Username đã tồn tại chưa
            var exists = await _userRepository.ExistsAsync(request.Email, request.Username);
            if (exists)
                throw new InvalidOperationException("Email hoặc Username đã được sử dụng.");

            // Hash password bằng BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Tạo mã xác minh 6 số ngẫu nhiên
            var code = GenerateVerificationCode();
            var expiry = DateTime.UtcNow.AddMinutes(_emailSettings.VerificationCodeExpiryMinutes);

            // Tạo user mới (chưa xác minh)
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Phone = request.Phone ?? string.Empty,
                Address = request.Address ?? string.Empty,
                Role = "Customer",
                IsEmailVerified = false,
                VerificationCode = code,
                VerificationCodeExpiry = expiry,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            // Gửi email chứa mã xác minh
            await _emailService.SendVerificationCodeAsync(user.Email, user.Username, code);

            return new
            {
                message = $"Đăng ký thành công! Mã xác minh đã được gửi đến {user.Email}. Vui lòng kiểm tra hộp thư.",
                email = user.Email,
                expiresInMinutes = _emailSettings.VerificationCodeExpiryMinutes
            };
        }

        // ─────────────────────────────────────────────────────────────
        // VERIFY EMAIL: nhập mã 6 số → xác minh tài khoản
        // ─────────────────────────────────────────────────────────────
        public async Task<object> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

            if (user.IsEmailVerified)
                return new { message = "Email đã được xác minh trước đó. Bạn có thể đăng nhập." };

            // Kiểm tra mã và thời hạn
            if (user.VerificationCode != request.Code)
                throw new InvalidOperationException("Mã xác minh không đúng.");

            if (user.VerificationCodeExpiry == null || user.VerificationCodeExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Mã xác minh đã hết hạn. Vui lòng yêu cầu mã mới.");

            // Cập nhật trạng thái xác minh trong database
            await _userRepository.MarkEmailVerifiedAsync(user.Id);

            return new { message = "Xác minh email thành công! Bạn có thể đăng nhập ngay." };
        }

        // ─────────────────────────────────────────────────────────────
        // RESEND CODE: gửi lại mã xác minh mới
        // ─────────────────────────────────────────────────────────────
        public async Task<object> ResendVerificationCodeAsync(ResendCodeRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

            if (user.IsEmailVerified)
                throw new InvalidOperationException("Email này đã được xác minh rồi.");

            // Tạo mã mới
            var newCode = GenerateVerificationCode();
            var expiry = DateTime.UtcNow.AddMinutes(_emailSettings.VerificationCodeExpiryMinutes);

            await _userRepository.UpdateVerificationAsync(user.Id, newCode, expiry);

            // Gửi email mới
            await _emailService.SendVerificationCodeAsync(user.Email, user.Username, newCode);

            return new
            {
                message = $"Đã gửi lại mã xác minh mới đến {user.Email}.",
                expiresInMinutes = _emailSettings.VerificationCodeExpiryMinutes
            };
        }

        // ─────────────────────────────────────────────────────────────
        // LOGIN: chỉ cho phép user đã xác minh email đăng nhập
        // ─────────────────────────────────────────────────────────────
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isValidPassword)
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

            // Chặn nếu chưa xác minh email
            if (!user.IsEmailVerified)
                throw new InvalidOperationException(
                    "Tài khoản chưa xác minh email. Vui lòng kiểm tra hộp thư và nhập mã xác minh.");

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

        // ─────────────────────────────────────────────────────────────
        // GET PROFILE
        // ─────────────────────────────────────────────────────────────
        public async Task<UserInfo> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng.");

            return new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsEmailVerified = user.IsEmailVerified
            };
        }

        // ─────────────────────────────────────────────────────────────
        // HELPER: tạo mã 6 chữ số ngẫu nhiên
        // ─────────────────────────────────────────────────────────────
        private static string GenerateVerificationCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        // ─────────────────────────────────────────────────────────────
        // FORGOT PASSWORD: gửi OTP đặt lại mật khẩu (không cần login)
        // ─────────────────────────────────────────────────────────────
        public async Task<object> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            // Không tiết lộ email có tồn tại hay không (tránh email enumeration attack)
            if (user == null || !user.IsEmailVerified)
                return new { message = $"Nếu email {request.Email} tồn tại, mã OTP sẽ được gửi đến hộp thư." };

            var code = GenerateVerificationCode();
            var expiry = DateTime.UtcNow.AddMinutes(_emailSettings.VerificationCodeExpiryMinutes);

            await _userRepository.UpdatePasswordResetCodeAsync(user.Id, code, expiry);
            await _emailService.SendPasswordResetCodeAsync(user.Email, user.Username, code);

            return new
            {
                message = $"Mã OTP đặt lại mật khẩu đã được gửi đến {request.Email}.",
                expiresInMinutes = _emailSettings.VerificationCodeExpiryMinutes
            };
        }

        // ─────────────────────────────────────────────────────────────
        // RESET PASSWORD: nhập OTP + mật khẩu mới (không cần login)
        // ─────────────────────────────────────────────────────────────
        public async Task<object> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

            if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != request.Code)
                throw new InvalidOperationException("Mã OTP không đúng.");

            if (user.PasswordResetCodeExpiry == null || user.PasswordResetCodeExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdatePasswordAsync(user.Id, newHash);

            return new { message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới." };
        }

        // ─────────────────────────────────────────────────────────────
        // CHANGE PASSWORD: đổi mật khẩu khi đã login (cần OTP + mk cũ)
        // ─────────────────────────────────────────────────────────────
        public async Task<object> RequestChangePasswordOtpAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng.");

            var code = GenerateVerificationCode();
            var expiry = DateTime.UtcNow.AddMinutes(_emailSettings.VerificationCodeExpiryMinutes);

            await _userRepository.UpdatePasswordResetCodeAsync(user.Id, code, expiry);
            await _emailService.SendPasswordResetCodeAsync(user.Email, user.Username, code);

            return new
            {
                message = $"Mã OTP xác nhận đổi mật khẩu đã được gửi đến {user.Email}.",
                expiresInMinutes = _emailSettings.VerificationCodeExpiryMinutes
            };
        }

        public async Task<object> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng.");

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu cũ không đúng.");

            // Kiểm tra OTP
            if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != request.OtpCode)
                throw new InvalidOperationException("Mã OTP không đúng.");

            if (user.PasswordResetCodeExpiry == null || user.PasswordResetCodeExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

            // Cập nhật mật khẩu mới
            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdatePasswordAsync(user.Id, newHash);

            return new { message = "Đổi mật khẩu thành công!" };
        }
    }
}
