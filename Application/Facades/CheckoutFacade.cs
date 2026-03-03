using swd.Application.DTOs.Order;
using swd.Domain.Interfaces; // Add this missing namespace
using swd.Application.Builders;
namespace swd.Application.Facades // Add this namespace
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
            var product = await _productRepo.GetByIdAsync(request.ProductId);

            if (product == null)
                throw new ArgumentException("Product not found");
            
            if (product.Inventory.Quantity < request.Quantity)
                throw new InvalidOperationException("Insufficient inventory");

            Promotion? promo = null;
            if (!string.IsNullOrEmpty(request.PromotionId))
            {
                promo = await _promoRepo.GetByIdAsync(request.PromotionId);
                if (promo == null || promo.Status != "Active" || DateTime.UtcNow < promo.StartAt || DateTime.UtcNow > promo.EndAt)
                    throw new ArgumentException("Invalid or expired promotion");
            }

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

            product.Inventory.Quantity -= request.Quantity;
            await _productRepo.UpdateAsync(product.Id, product);

            await _orderRepo.CreateAsync(order);

            return new CheckoutResponse
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status
            };
        }
    }
}