namespace swd.Application.DTOs.Order
{
    public class GetOrderResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
        public ShippingInfoDto? Shipping { get; set; }
        public PaymentInfoDto? Payment { get; set; }
        public PrescriptionInfoDto? Prescription { get; set; }
        public PromotionSnapshotDto? Promotion { get; set; }
        public ReturnRequestDto? ReturnRequest { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int WarrantyMonths { get; set; }
    }

    public class ShippingInfoDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class PaymentInfoDto
    {
        public string Method { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string OptionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class PrescriptionInfoDto
    {
        public EyePrescriptionDto? LeftEye { get; set; }
        public EyePrescriptionDto? RightEye { get; set; }
        public decimal? PupillaryDistance { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
        public string? VerifyStatus { get; set; }
    }

    public class PromotionSnapshotDto
    {
        public string PromotionId { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
    }

    public class ReturnRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
