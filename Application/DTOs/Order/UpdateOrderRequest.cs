namespace swd.Application.DTOs.Order
{
    public class UpdateOrderRequest
    {
        public string Status { get; set; } // Pending, Shipped, Completed, Cancelled
        public UpdateShippingInfo? Shipping { get; set; }
        public UpdatePaymentInfo? Payment { get; set; }
    }

    public class UpdateShippingInfo
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class UpdatePaymentInfo
    {
        public string? Method { get; set; }
        public string? Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}