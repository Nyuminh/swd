using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Cart
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    public List<CartItem> Items { get; set; } = new();

    public List<CartComboItem> ComboItems { get; set; } = new();

    public int Version { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CartItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; }

    public string ProductName { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}

public class CartComboItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ComboId { get; set; }

    public string ComboName { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? PromotionId { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
