using MongoDB.Bson.Serialization.Attributes;

public class ShippingOption
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Carrier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Fee { get; set; }

    public bool IsActive { get; set; } = true;

    public int EstimatedMinDays { get; set; } = 1;

    public int EstimatedMaxDays { get; set; } = 3;
}

public class PaymentOption
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public bool IsActive { get; set; } = true;
}
