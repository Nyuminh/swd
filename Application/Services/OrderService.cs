using swd.Application.DTOs.Order;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<List<GetOrderResponse>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<GetOrderResponse> GetOrderByIdAsync(string id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {id} not found.");

            return MapToResponse(order);
        }

        public async Task<List<GetOrderResponse>> GetOrdersByUserAsync(string userId)
        {
            var orders = await _orderRepository.GetByUserAsync(userId);
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<GetOrderResponse> UpdateOrderAsync(string id, UpdateOrderRequest request)
        {
            var existingOrder = await _orderRepository.GetByIdAsync(id);
            if (existingOrder == null)
                throw new KeyNotFoundException($"Order with ID {id} not found.");

            existingOrder.Status = request.Status ?? existingOrder.Status;
            existingOrder.UpdatedAt = DateTime.UtcNow;

            if (request.Shipping != null && existingOrder.Shipping != null)
            {
                existingOrder.Shipping.FullName = request.Shipping.FullName ?? existingOrder.Shipping.FullName;
                existingOrder.Shipping.Address = request.Shipping.Address ?? existingOrder.Shipping.Address;
                existingOrder.Shipping.Phone = request.Shipping.Phone ?? existingOrder.Shipping.Phone;
                existingOrder.Shipping.Method = request.Shipping.Method ?? existingOrder.Shipping.Method;
                existingOrder.Shipping.Status = request.Shipping.Status ?? existingOrder.Shipping.Status;
                existingOrder.Shipping.ShippedAt = request.Shipping.ShippedAt ?? existingOrder.Shipping.ShippedAt;
                existingOrder.Shipping.DeliveredAt = request.Shipping.DeliveredAt ?? existingOrder.Shipping.DeliveredAt;
            }

            if (request.Payment != null && existingOrder.Payment != null)
            {
                existingOrder.Payment.Method = request.Payment.Method ?? existingOrder.Payment.Method;
                existingOrder.Payment.Status = request.Payment.Status ?? existingOrder.Payment.Status;
                existingOrder.Payment.PaidAt = request.Payment.PaidAt ?? existingOrder.Payment.PaidAt;
            }

            await _orderRepository.UpdateAsync(id, existingOrder);
            return MapToResponse(existingOrder);
        }

        public async Task DeleteOrderAsync(string id)
        {
            var existingOrder = await _orderRepository.GetByIdAsync(id);
            if (existingOrder == null)
                throw new KeyNotFoundException($"Order with ID {id} not found.");

            await _orderRepository.DeleteAsync(id);
        }

        public async Task<swd.Application.DTOs.Dashboard.RevenueDashboardResponse> GetRevenueSummaryAsync()
        {
            // Usually, only count Completed or Delivered orders as realized revenue.
            var completedOrders = await _orderRepository.GetByStatusAsync("Completed");

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalOrders = completedOrders.Count;

            // Group by month, e.g., "YYYY-MM"
            var revenueByMonth = completedOrders
                .Where(o => o.CreatedAt != default) // fallback sanity check
                .GroupBy(o => o.CreatedAt.ToString("yyyy-MM"))
                .Select(g => new swd.Application.DTOs.Dashboard.MonthlyRevenueDto
                {
                    Month = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return new swd.Application.DTOs.Dashboard.RevenueDashboardResponse
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                RevenueByMonth = revenueByMonth
            };
        }

        private static GetOrderResponse MapToResponse(Order order)
        {
            return new GetOrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                Items = order.Items?.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    WarrantyMonths = item.WarrantyMonths
                }).ToList() ?? new List<OrderItemDto>(),
                Shipping = order.Shipping != null ? new ShippingInfoDto
                {
                    FullName = order.Shipping.FullName,
                    Address = order.Shipping.Address,
                    Phone = order.Shipping.Phone,
                    Carrier = order.Shipping.Carrier,
                    Method = order.Shipping.Method,
                    Fee = order.Shipping.Fee,
                    Status = order.Shipping.Status,
                    ShippedAt = order.Shipping.ShippedAt,
                    DeliveredAt = order.Shipping.DeliveredAt
                } : null,
                Payment = order.Payment != null ? new PaymentInfoDto
                {
                    Method = order.Payment.Method,
                    Status = order.Payment.Status,
                    PaidAt = order.Payment.PaidAt
                } : null,
                Promotion = order.Promotion != null ? new PromotionSnapshotDto
                {
                    PromotionId = order.Promotion.PromotionId,
                    DiscountPercent = order.Promotion.DiscountPercent
                } : null,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}
