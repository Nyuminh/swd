namespace swd.Application.Builders
{
    public class OrderBuilder
    {
        private Order _order = new Order
        {
            Items = new List<OrderItem>(),
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        public OrderBuilder SetUser(string userId)
        {
            _order.UserId = userId;
            return this;
        }

        public OrderBuilder AddItem(Product product, int quantity)
        {
            _order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity,
                WarrantyMonths = product.Warranty?.Months ?? 0
            });

            return this;
        }

        public OrderBuilder ApplyPromotion(Promotion promo)
        {
            _order.Promotion = new PromotionSnapshot
            {
                PromotionId = promo.Id,
                DiscountPercent = promo.DiscountPercent
            };

            return this;
        }

        public OrderBuilder SetShipping(ShippingInfo shipping)
        {
            _order.Shipping = shipping;
            return this;
        }

        public OrderBuilder SetPayment(string method)
        {
            _order.Payment = new PaymentInfo
            {
                Method = method,
                Status = "Pending"
            };
            return this;
        }

        public Order Build()
        {
            var total = _order.Items.Sum(x => x.Price * x.Quantity);

            if (_order.Promotion != null)
            {
                total -= total * (_order.Promotion.DiscountPercent / 100);
            }

            total += _order.Shipping?.Fee ?? 0;

            _order.TotalAmount = total;

            return _order;
        }
    }
}