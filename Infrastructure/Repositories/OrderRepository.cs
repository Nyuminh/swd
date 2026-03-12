using MongoDB.Driver;
using swd.Infrastructure.Persistence;
using swd.Domain.Interfaces;

namespace swd.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _collection;

        public OrderRepository(MongoDbContext context)
        {
            _collection = context.Orders;
        }

        public async Task<List<Order>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<Order> GetByIdAsync(string id) =>
            await _collection.Find(o => o.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Order entity) =>
            await _collection.InsertOneAsync(entity);

        public async Task UpdateAsync(string id, Order entity) =>
            await _collection.ReplaceOneAsync(o => o.Id == id, entity);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(o => o.Id == id);

        public async Task<List<Order>> GetByUserAsync(string userId) =>
            await _collection.Find(o => o.UserId == userId).ToListAsync();

        public async Task<List<Order>> GetByStatusAsync(string status) =>
            await _collection.Find(o => o.Status == status).ToListAsync();
    }
}