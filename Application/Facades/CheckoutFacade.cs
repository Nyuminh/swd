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

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required.", nameof(request.UserId));

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("At least one item is required.", nameof(request.Items));

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId))
                    throw new ArgumentException("ProductId is required for all items.");
                if (item.Quantity <= 0)
                    throw new ArgumentException($"Quantity must be greater than 0 for product {item.ProductId}.");
            }

            var reservedProducts = new System.Collections.Generic.List<(Product Product, int Quantity)>();

            foreach (var item in request.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    await RollbackInventory(reservedProducts);
                    throw new ArgumentException($"Product {item.ProductId} not found");
                }

                var inventoryReserved = await _productRepo.TryReserveInventoryAsync(product.Id, item.Quantity);
                if (!inventoryReserved)
                {
                    await RollbackInventory(reservedProducts);
                    throw new InvalidOperationException($"Insufficient inventory for product {product.Id}");
                }

                reservedProducts.Add((product, item.Quantity));
            }

            Promotion? promo = null;
            if (!string.IsNullOrEmpty(request.PromotionId))
            {
                promo = await _promoRepo.GetByIdAsync(request.PromotionId);
                if (promo == null
                    || !string.Equals(promo.Status, "Active", StringComparison.OrdinalIgnoreCase)
                    || DateTime.UtcNow < promo.StartAt
                    || DateTime.UtcNow > promo.EndAt)
                {
                    await RollbackInventory(reservedProducts);
                    throw new ArgumentException("Invalid or expired promotion");
                }
            }

            try
            {
                var builder = new OrderBuilder()
                    .SetUser(request.UserId)
                    .SetShipping(new ShippingInfo
                    {
                        FullName = request.Shipping.FullName,
                        Address = request.Shipping.Address,
                        Phone = request.Shipping.Phone,
                        Method = request.Shipping.Method,
                        Fee = 30000, // Fixed fee for now or could be calculated
                        Status = "Pending"
                    })
                    .SetPayment(request.PaymentMethod);

                if (request.Prescription != null)
                {
                    builder.SetPrescription(new PrescriptionInfo
                    {
                        LeftEye = request.Prescription.LeftEye ?? "",
                        RightEye = request.Prescription.RightEye ?? "",
                        Describe = request.Prescription.Describe ?? ""
                    });
                }

                foreach (var rp in reservedProducts)
                {
                    builder.AddItem(rp.Product, rp.Quantity);
                }

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
                await RollbackInventory(reservedProducts);
                throw;
            }
        }

        private async Task RollbackInventory(System.Collections.Generic.List<(Product Product, int Quantity)> reservedProducts)
        {
            foreach (var rp in reservedProducts)
            {
                await _productRepo.ReleaseInventoryAsync(rp.Product.Id, rp.Quantity);
            }
        }
    }
}
