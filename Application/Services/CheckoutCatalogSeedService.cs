using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class CheckoutCatalogSeedService
    {
        private readonly IRepository<ShippingOption> _shippingRepository;
        private readonly IRepository<PaymentOption> _paymentRepository;

        public CheckoutCatalogSeedService(
            IRepository<ShippingOption> shippingRepository,
            IRepository<PaymentOption> paymentRepository)
        {
            _shippingRepository = shippingRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task SeedAsync()
        {
            var shippingOptions = await _shippingRepository.GetAllAsync();
            if (shippingOptions.Count == 0)
            {
                await _shippingRepository.CreateAsync(new ShippingOption
                {
                    Id = "shipping-standard",
                    Carrier = "GHN",
                    Name = "Standard",
                    Fee = 30000,
                    EstimatedMinDays = 2,
                    EstimatedMaxDays = 4,
                    IsActive = true
                });

                await _shippingRepository.CreateAsync(new ShippingOption
                {
                    Id = "shipping-express",
                    Carrier = "GHN",
                    Name = "Express",
                    Fee = 45000,
                    EstimatedMinDays = 1,
                    EstimatedMaxDays = 2,
                    IsActive = true
                });
            }

            var paymentOptions = await _paymentRepository.GetAllAsync();
            if (paymentOptions.Count == 0)
            {
                await _paymentRepository.CreateAsync(new PaymentOption
                {
                    Id = "payment-cod",
                    Code = "COD",
                    DisplayName = "Cash on Delivery",
                    Provider = "Offline",
                    IsOnline = false,
                    IsActive = true
                });

                await _paymentRepository.CreateAsync(new PaymentOption
                {
                    Id = "payment-demo-card",
                    Code = "DEMO_CARD",
                    DisplayName = "Demo Card",
                    Provider = "DemoGateway",
                    IsOnline = true,
                    IsActive = true
                });
            }
        }
    }
}
