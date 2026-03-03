using MongoDB.Driver;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace swd.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IMongoCollection<Category> _collection;

        public CategoryRepository(MongoDbContext context)
        {
            _collection = context.Categories;
        }

        public async Task<List<Category>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<Category> GetByIdAsync(string id) =>
            await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Category entity) =>
            await _collection.InsertOneAsync(entity);

        public async Task UpdateAsync(string id, Category entity) =>
            await _collection.ReplaceOneAsync(c => c.Id == id, entity);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(c => c.Id == id);
    }
}