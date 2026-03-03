using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Combo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }

    public List<ComboProduct> Products { get; set; } = new();

    public decimal TotalPrice { get; set; }
}

public class ComboProduct
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}