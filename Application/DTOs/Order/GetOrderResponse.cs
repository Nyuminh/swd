namespace swd.Application.DTOs.Order
{
    public class GetOrderResponse
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public ShippingInfoDto Shipping { get; set; }
        public PaymentInfoDto Payment { get; set; }
        public PromotionSnapshotDto Promotion { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int WarrantyMonths { get; set; }
    }

    public class ShippingInfoDto
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Method { get; set; }
        public decimal Fee { get; set; }
        public string Status { get; set; }
    }

    public class PaymentInfoDto
    {
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class PromotionSnapshotDto
    {
        public string PromotionId { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}