using swd.Application.DTOs.Order;
using swd.Domain.Interfaces;

namespace swd.Application.Services
{
    public class PrescriptionService
    {
        private readonly IOrderRepository _orderRepository;

        public PrescriptionService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<GetOrderResponse> VerifyPrescriptionAsync(string orderId, VerifyPrescriptionRequest request)
        {
            var existingOrder = await _orderRepository.GetByIdAsync(orderId);
            if (existingOrder == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (existingOrder.Prescription == null)
                throw new InvalidOperationException("Order does not contain a prescription.");

            if (!string.Equals(existingOrder.Prescription.VerifyStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Prescription is already {existingOrder.Prescription.VerifyStatus}.");

            existingOrder.Prescription.VerifyStatus = request.IsApproved ? "Verified" : "Rejected";
            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                existingOrder.Prescription.Notes = request.Note;
            }

            if (!request.IsApproved && string.Equals(existingOrder.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                existingOrder.Status = "PrescriptionRejected";
            }

            existingOrder.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(orderId, existingOrder);

            return MapToResponse(existingOrder);
        }

        public async Task<PrescriptionInfoDto?> GetPrescriptionByOrderIdAsync(string orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            if (order.Prescription == null)
                return null;

            return MapPrescription(order.Prescription);
        }

        public async Task<List<GetOrderResponse>> GetOrdersPendingVerificationAsync()
        {
            var allOrders = await _orderRepository.GetAllAsync();
            var pending = allOrders
                .Where(o => o.Prescription != null
                    && string.Equals(o.Prescription.VerifyStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return pending.Select(MapToResponse).ToList();
        }

        private static PrescriptionInfoDto MapPrescription(PrescriptionInfo prescription)
        {
            return new PrescriptionInfoDto
            {
                LeftEye = prescription.LeftEye != null ? new EyePrescriptionDto
                {
                    Sphere = prescription.LeftEye.Sphere,
                    Cylinder = prescription.LeftEye.Cylinder,
                    Axis = prescription.LeftEye.Axis,
                    Add = prescription.LeftEye.Add
                } : null,
                RightEye = prescription.RightEye != null ? new EyePrescriptionDto
                {
                    Sphere = prescription.RightEye.Sphere,
                    Cylinder = prescription.RightEye.Cylinder,
                    Axis = prescription.RightEye.Axis,
                    Add = prescription.RightEye.Add
                } : null,
                PupillaryDistance = prescription.PupillaryDistance,
                ImageUrl = prescription.ImageUrl,
                Notes = prescription.Notes,
                VerifyStatus = prescription.VerifyStatus
            };
        }

        private static GetOrderResponse MapToResponse(Order order)
        {
            return new GetOrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items?.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    WarrantyMonths = i.WarrantyMonths
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
                Prescription = order.Prescription != null ? MapPrescription(order.Prescription) : null,
                Promotion = order.Promotion != null ? new PromotionSnapshotDto
                {
                    PromotionId = order.Promotion.PromotionId,
                    DiscountPercent = order.Promotion.DiscountPercent
                } : null,
                ReturnRequest = order.ReturnRequest != null ? new ReturnRequestDto
                {
                    Status = order.ReturnRequest.Status,
                    Reason = order.ReturnRequest.Reason,
                    CreatedAt = order.ReturnRequest.CreatedAt
                } : null
            };
        }
    }
}
