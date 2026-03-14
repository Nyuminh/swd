using swd.Application.DTOs.Promotion;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class PromotionService
    {
        private readonly IRepository<Promotion> _promotionRepository;

        public PromotionService(IRepository<Promotion> promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<List<Promotion>> GetAllAsync()
        {
            var promotions = await _promotionRepository.GetAllAsync();
            return promotions
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.StartAt)
                .ToList();
        }

        public async Task<Promotion> GetByIdAsync(string id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion is null)
                throw new KeyNotFoundException($"Promotion with id '{id}' was not found.");

            return promotion;
        }

        public async Task<Promotion> CreateAsync(CreatePromotionRequest request)
        {
            ValidateRequest(request.Name, request.DiscountPercent, request.StartAt, request.EndAt, request.Status);

            var promotion = new Promotion
            {
                Name = request.Name.Trim(),
                DiscountPercent = request.DiscountPercent,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Priority = request.Priority,
                Status = NormalizeStatus(request.Status)
            };

            await _promotionRepository.CreateAsync(promotion);
            return promotion;
        }

        public async Task<Promotion> UpdateAsync(string id, UpdatePromotionRequest request)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion is null)
                throw new KeyNotFoundException($"Promotion with id '{id}' was not found.");

            ValidateRequest(request.Name, request.DiscountPercent, request.StartAt, request.EndAt, request.Status);

            promotion.Name = request.Name.Trim();
            promotion.DiscountPercent = request.DiscountPercent;
            promotion.StartAt = request.StartAt;
            promotion.EndAt = request.EndAt;
            promotion.Priority = request.Priority;
            promotion.Status = NormalizeStatus(request.Status);

            await _promotionRepository.UpdateAsync(id, promotion);
            return promotion;
        }

        public async Task DeleteAsync(string id)
        {
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion is null)
                throw new KeyNotFoundException($"Promotion with id '{id}' was not found.");

            await _promotionRepository.DeleteAsync(id);
        }

        private static void ValidateRequest(
            string? name,
            decimal discountPercent,
            DateTime startAt,
            DateTime endAt,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentException("DiscountPercent must be between 0 and 100.", nameof(discountPercent));

            if (endAt < startAt)
                throw new ArgumentException("EndAt must be greater than or equal to StartAt.", nameof(endAt));

            _ = NormalizeStatus(status);
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                return "Active";

            if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
                return "Inactive";

            throw new ArgumentException("Status must be either 'Active' or 'Inactive'.", nameof(status));
        }
    }
}
