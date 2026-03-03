using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    public List<OrderItem> Items { get; set; } = new(); // Fixed: removed duplicate

    public ShippingInfo Shipping { get; set; }

    public PaymentInfo Payment { get; set; }

    public PrescriptionInfo Prescription { get; set; }

    public PromotionSnapshot Promotion { get; set; }

    public ReturnRequestInfo ReturnRequest { get; set; }

    public bool IsPreOrder { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string Status { get; set; } // Pending, Shipped, Completed, Cancelled

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Fixed: removed duplicate

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Fixed: removed duplicate
}
public class OrderItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; }

    public string ProductName { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int WarrantyMonths { get; set; }
}
public class ShippingInfo
{
    public string FullName { get; set; }

    public string Address { get; set; }

    public string Phone { get; set; }

    public string Carrier { get; set; }

    public string Method { get; set; }

    public decimal Fee { get; set; }

    public string Status { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
public class PaymentInfo
{
    public string Method { get; set; }

    public string Status { get; set; }

    public DateTime? PaidAt { get; set; }
}
public class PrescriptionInfo
{
    public string LeftEye { get; set; }

    public string RightEye { get; set; }

    public string Describe { get; set; }
}
public class PromotionSnapshot
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string PromotionId { get; set; }

    public decimal DiscountPercent { get; set; }
}
public class ReturnRequestInfo
{
    public string Status { get; set; }

    public string Reason { get; set; }

    public DateTime? CreatedAt { get; set; }
}