namespace swd.Application.DTOs.Order
{
    public class CheckoutRequest
    {
        public string UserId { get; set; }

        public string ProductId { get; set; }

        public int Quantity { get; set; }

        public string? PromotionId { get; set; }
    }
}