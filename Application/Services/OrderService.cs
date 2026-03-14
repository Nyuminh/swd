using swd.Application.DTOs.Order;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
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

            var nextStatus = request.Status ?? existingOrder.Status;
            if (ShouldReleaseInventory(existingOrder, nextStatus))
            {
                await ReleaseInventoryAsync(existingOrder);
            }

            existingOrder.Status = nextStatus;
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

            if (CanReleaseInventory(existingOrder))
            {
                await ReleaseInventoryAsync(existingOrder);
            }

            await _orderRepository.DeleteAsync(id);
        }

        public async Task<GetOrderResponse> ConfirmDemoPaymentAsync(string id, ConfirmDemoPaymentRequest request)
        {
            var existingOrder = await _orderRepository.GetByIdAsync(id);
            if (existingOrder == null)
                throw new KeyNotFoundException($"Order with ID {id} not found.");

            if (existingOrder.Payment == null)
                throw new InvalidOperationException("Order does not have payment information.");

            if (string.Equals(request.Outcome, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                existingOrder.Payment.Status = "Paid";
                existingOrder.Payment.PaidAt = DateTime.UtcNow;
                existingOrder.Payment.TransactionReference = string.IsNullOrWhiteSpace(request.TransactionReference)
                    ? $"demo-{Guid.NewGuid():N}"
                    : request.TransactionReference.Trim();
                existingOrder.Payment.FailureReason = string.Empty;

                if (string.Equals(existingOrder.Status, "AwaitingPayment", StringComparison.OrdinalIgnoreCase))
                {
                    existingOrder.Status = "Pending";
                }
            }
            else if (string.Equals(request.Outcome, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                existingOrder.Payment.Status = "Failed";
                existingOrder.Payment.FailureReason = request.FailureReason?.Trim() ?? "Demo payment failed.";
                existingOrder.Payment.TransactionReference = request.TransactionReference?.Trim() ?? string.Empty;
                existingOrder.Payment.PaidAt = null;
                existingOrder.Status = "PaymentFailed";

                if (CanReleaseInventory(existingOrder))
                {
                    await ReleaseInventoryAsync(existingOrder);
                }
            }
            else
            {
                throw new ArgumentException("Outcome must be either 'Succeeded' or 'Failed'.", nameof(request.Outcome));
            }

            existingOrder.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(id, existingOrder);
            return MapToResponse(existingOrder);
        }

        public async Task<swd.Application.DTOs.Dashboard.RevenueDashboardResponse> GetRevenueSummaryAsync()
        {
            var completedOrders = await _orderRepository.GetByStatusAsync("Completed");
            completedOrders = completedOrders
                .Where(IsRealizedRevenueOrder)
                .ToList();

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
                    Provider = order.Payment.Provider,
                    OptionId = order.Payment.OptionId,
                    Status = order.Payment.Status,
                    TransactionReference = order.Payment.TransactionReference,
                    FailureReason = order.Payment.FailureReason,
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

        private static bool IsRealizedRevenueOrder(Order order)
        {
            if (order.Payment == null)
                return false;

            if (string.Equals(order.Payment.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(order.Payment.Method, "COD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Payment.Method, "Cash on Delivery", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldReleaseInventory(Order order, string nextStatus)
        {
            return CanReleaseInventory(order)
                && string.Equals(nextStatus, "Cancelled", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanReleaseInventory(Order order)
        {
            if (order.InventoryReleasedAt.HasValue)
                return false;

            return !string.Equals(order.Status, "Shipped", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ReleaseInventoryAsync(Order order)
        {
            foreach (var item in order.Items ?? Enumerable.Empty<OrderItem>())
            {
                if (string.IsNullOrWhiteSpace(item.ProductId) || item.Quantity <= 0)
                    continue;

                await _productRepository.ReleaseInventoryAsync(item.ProductId, item.Quantity);
            }

            order.InventoryReleasedAt = DateTime.UtcNow;
        }
    }
}
