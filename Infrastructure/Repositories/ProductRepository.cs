using MongoDB.Driver;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private const string InventoryQuantityPath = $"{nameof(Product.Inventory)}.{nameof(InventoryInfo.Quantity)}";

    private readonly IMongoCollection<Product> _collection;

    public ProductRepository(MongoDbContext context)
    {
        _collection = context.Products;
    }

    public async Task<List<Product>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Product> GetByIdAsync(string id) =>
        await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Product entity) =>
        await _collection.InsertOneAsync(entity);

    public async Task UpdateAsync(string id, Product entity) =>
        await _collection.ReplaceOneAsync(p => p.Id == id, entity);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(p => p.Id == id);

    public async Task<List<Product>> GetByCategoryAsync(string categoryId) =>
        await _collection.Find(p => p.CategoryId == categoryId).ToListAsync();

    public async Task<bool> TryReserveInventoryAsync(string id, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0.");

        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, id),
            Builders<Product>.Filter.Gte(InventoryQuantityPath, quantity));
        var update = Builders<Product>.Update.Inc(InventoryQuantityPath, -quantity);
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task ReleaseInventoryAsync(string id, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0.");

        var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
        var update = Builders<Product>.Update.Inc(InventoryQuantityPath, quantity);
        await _collection.UpdateOneAsync(filter, update);
    }
}
