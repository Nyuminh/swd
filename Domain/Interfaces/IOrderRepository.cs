namespace swd.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<List<Order>> GetByUserAsync(string userId);
        Task<List<Order>> GetByStatusAsync(string status);
    }
}