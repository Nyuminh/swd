using System.Linq.Expressions;
using MongoDB.Driver;
using swd.Domain.Interfaces;
using swd.Infrastructure.Persistence;

namespace swd.Infrastructure.Repositories
{
    internal static class RepositoryIdFilter
    {
        public static FilterDefinition<T> Create<T>(string id) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null || idProperty.PropertyType != typeof(string))
                throw new InvalidOperationException($"{typeof(T).Name} must expose a string Id property.");

            var entity = Expression.Parameter(typeof(T), "entity");
            var property = Expression.Property(entity, idProperty);
            var lambda = Expression.Lambda<Func<T, string>>(property, entity);

            return Builders<T>.Filter.Eq(lambda, id);
        }
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public Repository(MongoDbContext context)
        {
            var collectionName = typeof(T).Name + "s";
            _collection = context.Database.GetCollection<T>(collectionName);
        }

        public async Task<List<T>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<T> GetByIdAsync(string id) =>
            await _collection.Find(RepositoryIdFilter.Create<T>(id)).FirstOrDefaultAsync();

        public async Task CreateAsync(T entity) =>
            await _collection.InsertOneAsync(entity);

        public async Task UpdateAsync(string id, T entity) =>
            await _collection.ReplaceOneAsync(RepositoryIdFilter.Create<T>(id), entity);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(RepositoryIdFilter.Create<T>(id));
    }
}
