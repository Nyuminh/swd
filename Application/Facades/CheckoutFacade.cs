using swd.Application.Builders;
using swd.Application.DTOs.Order;
using swd.Domain.Interfaces;

namespace swd.Application.Facades
{
    public class CheckoutFacade
    {
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IRepository<Promotion> _promoRepo;

        public CheckoutFacade(
            IProductRepository productRepo,
            IOrderRepository orderRepo,
            IRepository<Promotion> promoRepo)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _promoRepo = promoRepo;
        }

        public async Task<CheckoutResponse> PlaceOrder(CheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.", nameof(request.Quantity));

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required.", nameof(request.UserId));

            if (string.IsNullOrWhiteSpace(request.ProductId))
                throw new ArgumentException("ProductId is required.", nameof(request.ProductId));

            var product = await _productRepo.GetByIdAsync(request.ProductId);
            if (product == null)
                throw new ArgumentException("Product not found");

            Promotion? promo = null;
            if (!string.IsNullOrEmpty(request.PromotionId))
            {
                promo = await _promoRepo.GetByIdAsync(request.PromotionId);
                if (promo == null
                    || !string.Equals(promo.Status, "Active", StringComparison.OrdinalIgnoreCase)
                    || DateTime.UtcNow < promo.StartAt
                    || DateTime.UtcNow > promo.EndAt)
                {
                    throw new ArgumentException("Invalid or expired promotion");
                }
            }

            var inventoryReserved = await _productRepo.TryReserveInventoryAsync(product.Id, request.Quantity);
            if (!inventoryReserved)
                throw new InvalidOperationException("Insufficient inventory");

            try
            {
                var builder = new OrderBuilder()
                    .SetUser(request.UserId)
                    .AddItem(product, request.Quantity)
                    .SetShipping(new ShippingInfo
                    {
                        Address = "HCM",
                        Phone = "0909xxx",
                        Method = "Standard",
                        Fee = 30000
                    })
                    .SetPayment("COD");

                if (promo != null)
                    builder.ApplyPromotion(promo);

                var order = builder.Build();
                await _orderRepo.CreateAsync(order);

                return new CheckoutResponse
                {
                    OrderId = order.Id,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status
                };
            }
            catch
            {
                await _productRepo.ReleaseInventoryAsync(product.Id, request.Quantity);
                throw;
            }
        }
    }
}
