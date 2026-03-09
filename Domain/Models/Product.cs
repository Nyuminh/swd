using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; }

    public string ProductType { get; set; }

    public decimal Price { get; set; }

    public string Size { get; set; }

    public string Color { get; set; }

    public string TargetGender { get; set; }

    public InventoryInfo Inventory { get; set; }

    public List<ProductImage> Images { get; set; }

    public WarrantyInfo Warranty { get; set; }

    public FrameDetails? FrameDetails { get; set; }

    public LensDetails? LensDetails { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string PromotionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryInfo
{
    public int Quantity { get; set; }

    public int LowStockThreshold { get; set; }
}

public class ProductImage
{
    public string Url { get; set; }
}

public class WarrantyInfo
{
    public int Months { get; set; }

    public string Description { get; set; }
}

public class FrameDetails
{
    public string FrameShape { get; set; } = string.Empty;

    public string FitType { get; set; } = string.Empty;

    public List<string> StyleTags { get; set; } = new();

    public string FrameMaterial { get; set; } = string.Empty;
}

public class LensDetails
{
    public string LensType { get; set; } = string.Empty;

    public string Index { get; set; } = string.Empty;

    public List<string> Coatings { get; set; } = new();
}
