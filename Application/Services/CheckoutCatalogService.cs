using swd.Application.DTOs.Order;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class CheckoutCatalogService
    {
        private readonly IRepository<ShippingOption> _shippingRepository;
        private readonly IRepository<PaymentOption> _paymentRepository;

        public CheckoutCatalogService(
            IRepository<ShippingOption> shippingRepository,
            IRepository<PaymentOption> paymentRepository)
        {
            _shippingRepository = shippingRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<CheckoutOptionsResponse> GetActiveOptionsAsync()
        {
            var shippingOptions = await _shippingRepository.GetAllAsync();
            var paymentOptions = await _paymentRepository.GetAllAsync();

            return new CheckoutOptionsResponse
            {
                ShippingOptions = shippingOptions
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.Fee)
                    .Select(option => new CheckoutShippingOptionDto
                    {
                        Id = option.Id,
                        Carrier = option.Carrier,
                        Name = option.Name,
                        Fee = option.Fee,
                        EstimatedMinDays = option.EstimatedMinDays,
                        EstimatedMaxDays = option.EstimatedMaxDays
                    })
                    .ToList(),
                PaymentOptions = paymentOptions
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.IsOnline)
                    .Select(option => new CheckoutPaymentOptionDto
                    {
                        Id = option.Id,
                        Code = option.Code,
                        DisplayName = option.DisplayName,
                        Provider = option.Provider,
                        IsOnline = option.IsOnline
                    })
                    .ToList()
            };
        }
    }
}
