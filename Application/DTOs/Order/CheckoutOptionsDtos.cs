namespace swd.Application.DTOs.Order
{
    public class CheckoutOptionsResponse
    {
        public List<CheckoutShippingOptionDto> ShippingOptions { get; set; } = new();

        public List<CheckoutPaymentOptionDto> PaymentOptions { get; set; } = new();
    }

    public class CheckoutShippingOptionDto
    {
        public string Id { get; set; } = string.Empty;

        public string Carrier { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Fee { get; set; }

        public int EstimatedMinDays { get; set; }

        public int EstimatedMaxDays { get; set; }
    }

    public class CheckoutPaymentOptionDto
    {
        public string Id { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public bool IsOnline { get; set; }
    }

    public class ConfirmDemoPaymentRequest
    {
        public string Outcome { get; set; } = string.Empty;

        public string? TransactionReference { get; set; }

        public string? FailureReason { get; set; }
    }
}
