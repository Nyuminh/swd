using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Promotion
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } // Active, Inactive

    // Fixed: proper computed property
    public bool IsActive => Status == "Active" && DateTime.UtcNow >= StartAt && DateTime.UtcNow <= EndAt;
}