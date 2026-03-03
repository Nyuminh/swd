using MongoDB.Driver;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;
public class ProductRepository : IProductRepository
{
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
}