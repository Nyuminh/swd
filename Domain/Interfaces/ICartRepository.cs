namespace swd.Domain.Interfaces
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart> GetByUserIdAsync(string userId);
    }
}
