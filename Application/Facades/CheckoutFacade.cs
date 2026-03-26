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
        private readonly IRepository<ShippingOption> _shippingRepo;
        private readonly IRepository<PaymentOption> _paymentRepo;
        private readonly IUserRepository _userRepo;

        public CheckoutFacade(
            IProductRepository productRepo,
            IOrderRepository orderRepo,
            IRepository<Promotion> promoRepo,
            IRepository<ShippingOption> shippingRepo,
            IRepository<PaymentOption> paymentRepo,
            IUserRepository userRepo)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _promoRepo = promoRepo;
            _shippingRepo = shippingRepo;
            _paymentRepo = paymentRepo;
            _userRepo = userRepo;
        }

        public async Task<CheckoutResponse> PlaceOrder(CheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required.", nameof(request.UserId));

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("At least one item is required.", nameof(request.Items));

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(request.IdempotencyKey));

            if (string.IsNullOrWhiteSpace(request.ShippingOptionId))
                throw new ArgumentException("ShippingOptionId is required.", nameof(request.ShippingOptionId));

            if (string.IsNullOrWhiteSpace(request.PaymentOptionId))
                throw new ArgumentException("PaymentOptionId is required.", nameof(request.PaymentOptionId));

            var existingOrder = await _orderRepo.GetByUserIdAndIdempotencyKeyAsync(
                request.UserId,
                request.IdempotencyKey.Trim());
            if (existingOrder != null)
            {
                return BuildResponse(existingOrder);
            }

            var shippingOption = await _shippingRepo.GetByIdAsync(request.ShippingOptionId);
            if (shippingOption == null || !shippingOption.IsActive)
                throw new ArgumentException("Invalid or inactive shipping option.", nameof(request.ShippingOptionId));

            var paymentOption = await _paymentRepo.GetByIdAsync(request.PaymentOptionId);
            if (paymentOption == null || !paymentOption.IsActive)
                throw new ArgumentException("Invalid or inactive payment option.", nameof(request.PaymentOptionId));

            var user = await _userRepo.GetByIdAsync(request.UserId);
            var shippingContact = ResolveShippingContact(request.Shipping, user);

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
                    .SetIdempotencyKey(request.IdempotencyKey.Trim())
                    .SetShipping(new ShippingInfo
                    {
                        FullName = shippingContact.FullName,
                        Address = shippingContact.Address,
                        Phone = shippingContact.Phone,
                        Carrier = shippingOption.Carrier,
                        Method = shippingOption.Name,
                        Fee = shippingOption.Fee,
                        Status = "Pending"
                    })
                    .SetPayment(
                        paymentOption.DisplayName,
                        paymentOption.Provider,
                        paymentOption.Id)
                    .SetStatus(paymentOption.IsOnline ? "AwaitingPayment" : "Pending");

                if (request.Prescription != null)
                {
                    builder.SetPrescription(new PrescriptionInfo
                    {
                        LeftEye = request.Prescription.LeftEye != null ? new EyePrescription
                        {
                            Sphere = request.Prescription.LeftEye.Sphere,
                            Cylinder = request.Prescription.LeftEye.Cylinder,
                            Axis = request.Prescription.LeftEye.Axis,
                            Add = request.Prescription.LeftEye.Add
                        } : new EyePrescription(),
                        RightEye = request.Prescription.RightEye != null ? new EyePrescription
                        {
                            Sphere = request.Prescription.RightEye.Sphere,
                            Cylinder = request.Prescription.RightEye.Cylinder,
                            Axis = request.Prescription.RightEye.Axis,
                            Add = request.Prescription.RightEye.Add
                        } : new EyePrescription(),
                        PupillaryDistance = request.Prescription.PupillaryDistance,
                        ImageUrl = request.Prescription.ImageUrl ?? string.Empty,
                        Notes = request.Prescription.Notes ?? string.Empty,
                        VerifyStatus = "Pending"
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
                    Status = order.Status,
                    PaymentStatus = order.Payment?.Status ?? string.Empty,
                    PaymentOptionId = order.Payment?.OptionId ?? string.Empty,
                    PaymentReference = order.Payment?.TransactionReference ?? string.Empty,
                    ShippingFee = order.Shipping?.Fee ?? 0
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

        private static CheckoutShippingRequest ResolveShippingContact(CheckoutShippingRequest? request, User? user)
        {
            var fullName = string.IsNullOrWhiteSpace(request?.FullName)
                ? user?.Username?.Trim() ?? string.Empty
                : request.FullName.Trim();
            var address = string.IsNullOrWhiteSpace(request?.Address)
                ? user?.Address?.Trim() ?? string.Empty
                : request.Address.Trim();
            var phone = string.IsNullOrWhiteSpace(request?.Phone)
                ? user?.Phone?.Trim() ?? string.Empty
                : request.Phone.Trim();

            if (string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(address)
                || string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException("Shipping recipient information is required.", nameof(request));
            }

            return new CheckoutShippingRequest
            {
                FullName = fullName,
                Address = address,
                Phone = phone
            };
        }

        private static CheckoutResponse BuildResponse(Order order)
        {
            return new CheckoutResponse
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.Payment?.Status ?? string.Empty,
                PaymentOptionId = order.Payment?.OptionId ?? string.Empty,
                PaymentReference = order.Payment?.TransactionReference ?? string.Empty,
                ShippingFee = order.Shipping?.Fee ?? 0
            };
        }
    }
}
