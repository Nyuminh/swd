using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; } // Stored as BCrypt hash

    public string Phone { get; set; }

    public string Address { get; set; }

    public string Role { get; set; } = "Customer"; // Admin, Customer

    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiry { get; set; }

    public string? PasswordResetCode { get; set; }
    public DateTime? PasswordResetCodeExpiry { get; set; }

    // Any token issued at or before this timestamp is rejected.
    public DateTime? TokenInvalidBeforeUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
