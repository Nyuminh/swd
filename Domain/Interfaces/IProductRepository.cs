using System.Collections.Generic;
using System.Threading.Tasks;
namespace swd.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<List<Product>> GetByCategoryAsync(string categoryId);
    }
}