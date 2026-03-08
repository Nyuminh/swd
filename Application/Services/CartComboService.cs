using swd.Application.DTOs.CartCombo;
using swd.Application.Exceptions;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class CartComboService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IRepository<Combo> _comboRepository;
        private readonly IRepository<Promotion> _promotionRepository;

        public CartComboService(
            ICartRepository cartRepository,
            IRepository<Combo> comboRepository,
            IRepository<Promotion> promotionRepository)
        {
            _cartRepository = cartRepository;
            _comboRepository = comboRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<CartComboResponse> GetCartByUserIdAsync(string userId)
        {
            ValidateRequired(userId, nameof(userId));

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart is null)
            {
                throw new KeyNotFoundException($"Cart for user '{userId}' was not found.");
            }

            cart.ComboItems ??= new List<CartComboItem>();
            return MapToResponse(cart);
        }

        public async Task<CartComboResponse> AddComboAsync(string userId, CreateCartComboRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(request.ComboId, nameof(request.ComboId));
            ValidateQuantity(request.Quantity);

            var combo = await GetRequiredComboAsync(request.ComboId);
            var promotion = await ResolvePromotionAsync(request.PromotionId, request.UseBestPromotion);

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 0
                };
            }
            else
            {
                ValidateExpectedVersion(cart, request.ExpectedVersion);
            }

            cart.ComboItems ??= new List<CartComboItem>();

            var comboItem = cart.ComboItems.FirstOrDefault(x => x.ComboId == combo.Id);
            if (comboItem is null)
            {
                comboItem = new CartComboItem
                {
                    ComboId = combo.Id,
                    Quantity = 0
                };
                cart.ComboItems.Add(comboItem);
            }

            comboItem.Quantity += request.Quantity;
            ApplyPricing(comboItem, combo, promotion);

            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);

            return MapToResponse(cart);
        }

        public async Task<CartComboResponse> UpdateComboAsync(string userId, string comboId, UpdateCartComboRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(comboId, nameof(comboId));
            ValidateQuantity(request.Quantity);

            var cart = await GetRequiredCartAsync(userId);
            ValidateExpectedVersion(cart, request.ExpectedVersion);
            cart.ComboItems ??= new List<CartComboItem>();

            var comboItem = cart.ComboItems.FirstOrDefault(x => x.ComboId == comboId);
            if (comboItem is null)
            {
                throw new KeyNotFoundException($"Combo '{comboId}' was not found in cart.");
            }

            var combo = await GetRequiredComboAsync(comboId);
            var promotion = await ResolvePromotionAsync(request.PromotionId, request.UseBestPromotion);

            comboItem.Quantity = request.Quantity;
            ApplyPricing(comboItem, combo, promotion);

            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);

            return MapToResponse(cart);
        }

        public async Task<CartComboResponse> RemoveComboAsync(string userId, string comboId, int? expectedVersion = null)
        {
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(comboId, nameof(comboId));

            var cart = await GetRequiredCartAsync(userId);
            ValidateExpectedVersion(cart, expectedVersion);
            cart.ComboItems ??= new List<CartComboItem>();

            var removedCount = cart.ComboItems.RemoveAll(x => x.ComboId == comboId);
            if (removedCount == 0)
            {
                throw new KeyNotFoundException($"Combo '{comboId}' was not found in cart.");
            }

            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);

            return MapToResponse(cart);
        }

        public async Task ClearCartAsync(string userId, int? expectedVersion = null)
        {
            ValidateRequired(userId, nameof(userId));

            var cart = await GetRequiredCartAsync(userId);
            ValidateExpectedVersion(cart, expectedVersion);
            await _cartRepository.DeleteAsync(cart.Id);
        }

        private async Task<Cart> GetRequiredCartAsync(string userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart is null)
            {
                throw new KeyNotFoundException($"Cart for user '{userId}' was not found.");
            }

            return cart;
        }

        private async Task<Combo> GetRequiredComboAsync(string comboId)
        {
            var combo = await _comboRepository.GetByIdAsync(comboId);
            if (combo is null)
            {
                throw new KeyNotFoundException($"Combo with id '{comboId}' was not found.");
            }

            return combo;
        }

        private async Task<Promotion?> ResolvePromotionAsync(string? promotionId, bool useBestPromotion)
        {
            if (!string.IsNullOrWhiteSpace(promotionId))
            {
                var selectedPromotion = await _promotionRepository.GetByIdAsync(promotionId);
                if (selectedPromotion is null || !IsPromotionActive(selectedPromotion))
                {
                    throw new InvalidOperationException("Invalid or expired promotion.");
                }

                return selectedPromotion;
            }

            if (!useBestPromotion)
            {
                return null;
            }

            var promotions = await _promotionRepository.GetAllAsync();
            return promotions
                .Where(IsPromotionActive)
                .OrderByDescending(x => x.DiscountPercent)
                .ThenByDescending(x => x.Priority)
                .FirstOrDefault();
        }

        private async Task SaveCartAsync(Cart cart)
        {
            if (string.IsNullOrWhiteSpace(cart.Id))
            {
                await _cartRepository.CreateAsync(cart);
                return;
            }

            await _cartRepository.UpdateAsync(cart.Id, cart);
        }

        private static void ApplyPricing(CartComboItem item, Combo combo, Promotion? promotion)
        {
            var discountPercent = promotion?.DiscountPercent ?? 0m;
            var finalUnitPrice = combo.TotalPrice - (combo.TotalPrice * discountPercent / 100m);

            if (finalUnitPrice < 0m)
            {
                finalUnitPrice = 0m;
            }

            item.ComboId = combo.Id;
            item.ComboName = combo.Name;
            item.UnitPrice = combo.TotalPrice;
            item.PromotionId = promotion?.Id;
            item.DiscountPercent = discountPercent;
            item.FinalUnitPrice = finalUnitPrice;
            item.LineTotal = finalUnitPrice * item.Quantity;
        }

        private static bool IsPromotionActive(Promotion promotion)
        {
            if (!string.Equals(promotion.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return now >= promotion.StartAt && now <= promotion.EndAt;
        }

        private static void ValidateExpectedVersion(Cart cart, int? expectedVersion)
        {
            if (!expectedVersion.HasValue)
            {
                return;
            }

            if (cart.Version != expectedVersion.Value)
            {
                throw new ConcurrencyException("Cart has been modified. Please refresh and retry.");
            }
        }

        private static CartComboResponse MapToResponse(Cart cart)
        {
            var comboItems = cart.ComboItems ?? new List<CartComboItem>();
            var items = comboItems.Select(MapItem).ToList();

            var subTotal = items.Sum(x => x.UnitPrice * x.Quantity);
            var grandTotal = items.Sum(x => x.LineTotal);

            return new CartComboResponse
            {
                CartId = cart.Id ?? string.Empty,
                UserId = cart.UserId,
                Version = cart.Version,
                Items = items,
                SubTotal = subTotal,
                DiscountTotal = subTotal - grandTotal,
                GrandTotal = grandTotal,
                UpdatedAt = cart.UpdatedAt
            };
        }

        private static CartComboItemResponse MapItem(CartComboItem item)
        {
            return new CartComboItemResponse
            {
                ComboId = item.ComboId,
                ComboName = item.ComboName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                PromotionId = item.PromotionId,
                DiscountPercent = item.DiscountPercent,
                FinalUnitPrice = item.FinalUnitPrice,
                LineTotal = item.LineTotal
            };
        }

        private static void ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{fieldName} is required.");
            }
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than 0.");
            }
        }
    }
}
