namespace swd.Application.DTOs.Order
{
    public class CheckoutResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentOptionId { get; set; } = string.Empty;
        public string PaymentReference { get; set; } = string.Empty;
        public decimal ShippingFee { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = "Order placed successfully";
    }
}
