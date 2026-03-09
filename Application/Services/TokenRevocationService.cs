using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class TokenRevocationService
    {
        private readonly IUserRepository _userRepository;

        public TokenRevocationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RevokeUserTokensAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            await _userRepository.SetTokenInvalidBeforeAsync(userId, DateTime.UtcNow);
        }

        public async Task<bool> IsTokenRevokedAsync(string userId, DateTime issuedAtUtc)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return true;

            if (!user.TokenInvalidBeforeUtc.HasValue)
                return false;

            return issuedAtUtc <= user.TokenInvalidBeforeUtc.Value;
        }
    }
}
