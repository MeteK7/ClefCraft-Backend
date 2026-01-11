using ClefCraft.Domain.Common;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IReadOnlyList<T>> GetAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdReadOnlyAsync(int id);
        Task CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}