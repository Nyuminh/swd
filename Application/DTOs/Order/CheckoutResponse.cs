namespace swd.Application.DTOs.Order
{
    public class CheckoutResponse
    {
        public string OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = "Order placed successfully";
    }
}