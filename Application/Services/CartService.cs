using swd.Application.DTOs.Cart;
using swd.Application.Exceptions;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class CartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        public CartService(
            ICartRepository cartRepository,
            IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public async Task<CartResponse> GetCartByUserIdAsync(string userId)
        {
            ValidateRequired(userId, nameof(userId));

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart is null)
            {
                throw new KeyNotFoundException($"Cart for user '{userId}' was not found.");
            }

            cart.Items ??= new List<CartItem>();
            return MapToResponse(cart);
        }

        public async Task<CartResponse> AddToCartAsync(string userId, AddToCartRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(request.ProductId, nameof(request.ProductId));
            ValidateQuantity(request.Quantity);

            var product = await GetRequiredProductAsync(request.ProductId);

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 0,
                    Items = new List<CartItem>()
                };
            }
            else
            {
                ValidateExpectedVersion(cart, request.ExpectedVersion);
            }

            cart.Items ??= new List<CartItem>();

            var item = cart.Items.FirstOrDefault(x => x.ProductId == product.Id);
            if (item is null)
            {
                item = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price, // Capture real price
                    Quantity = 0
                };
                cart.Items.Add(item);
            }
            // Update product name/price to newest value just in case
            item.ProductName = product.Name;
            item.Price = product.Price;

            item.Quantity += request.Quantity;

            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);

            return MapToResponse(cart);
        }

        public async Task<CartResponse> UpdateCartItemAsync(string userId, string productId, UpdateCartItemRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(productId, nameof(productId));
            ValidateQuantity(request.Quantity);

            var cart = await GetRequiredCartAsync(userId);
            ValidateExpectedVersion(cart, request.ExpectedVersion);
            cart.Items ??= new List<CartItem>();

            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);
            if (item is null)
            {
                throw new KeyNotFoundException($"Product '{productId}' was not found in cart.");
            }

            var product = await GetRequiredProductAsync(productId);
            
            // Sync current state
            item.ProductName = product.Name;
            item.Price = product.Price;
            item.Quantity = request.Quantity;

            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);

            return MapToResponse(cart);
        }

        public async Task<CartResponse> RemoveFromCartAsync(string userId, string productId, int? expectedVersion = null)
        {
            ValidateRequired(userId, nameof(userId));
            ValidateRequired(productId, nameof(productId));

            var cart = await GetRequiredCartAsync(userId);
            ValidateExpectedVersion(cart, expectedVersion);
            cart.Items ??= new List<CartItem>();

            var removedCount = cart.Items.RemoveAll(x => x.ProductId == productId);
            if (removedCount == 0)
            {
                throw new KeyNotFoundException($"Product '{productId}' was not found in cart.");
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
            // clear both items and comboItems or only items? Standard approach implies all items
            cart.Items?.Clear();
            cart.ComboItems?.Clear();
            
            cart.UpdatedAt = DateTime.UtcNow;
            cart.Version += 1;
            await SaveCartAsync(cart);
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

        private async Task<Product> GetRequiredProductAsync(string productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
            {
                throw new KeyNotFoundException($"Product with id '{productId}' was not found.");
            }

            return product;
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

        private static CartResponse MapToResponse(Cart cart)
        {
            var items = cart.Items ?? new List<CartItem>();
            var responseItems = items.Select(MapItem).ToList();

            var grandTotal = responseItems.Sum(x => x.LineTotal);

            return new CartResponse
            {
                CartId = cart.Id ?? string.Empty,
                UserId = cart.UserId,
                Version = cart.Version,
                Items = responseItems,
                GrandTotal = grandTotal,
                UpdatedAt = cart.UpdatedAt
            };
        }

        private static CartItemResponse MapItem(CartItem item)
        {
            return new CartItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                LineTotal = item.Price * item.Quantity
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
