using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public string Address { get; set; }

    public string Role { get; set; } // Admin, Customer

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Fixed: removed duplicate
}
