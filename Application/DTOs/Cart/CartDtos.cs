namespace swd.Application.DTOs.Cart
{
    public class CartResponse
    {
        public string CartId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CartItemResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? ExpectedVersion { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
        public int? ExpectedVersion { get; set; }
    }
}
