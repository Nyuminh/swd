using MongoDB.Driver;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;

namespace swd.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly IMongoCollection<Cart> _collection;

        public CartRepository(MongoDbContext context)
        {
            _collection = context.Carts;
            EnsureIndexes();
        }

        public async Task<List<Cart>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<Cart> GetByIdAsync(string id) =>
            await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();

        public async Task<Cart> GetByUserIdAsync(string userId) =>
            await _collection.Find(c => c.UserId == userId).FirstOrDefaultAsync();

        public async Task CreateAsync(Cart entity) =>
            await _collection.InsertOneAsync(entity);

        public async Task UpdateAsync(string id, Cart entity) =>
            await _collection.ReplaceOneAsync(c => c.Id == id, entity);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(c => c.Id == id);

        private void EnsureIndexes()
        {
            var userIdIndex = new CreateIndexModel<Cart>(
                Builders<Cart>.IndexKeys.Ascending(c => c.UserId),
                new CreateIndexOptions
                {
                    Name = "ux_carts_user_id",
                    Unique = true
                });

            _collection.Indexes.CreateOne(userIdIndex);
        }
    }
}
