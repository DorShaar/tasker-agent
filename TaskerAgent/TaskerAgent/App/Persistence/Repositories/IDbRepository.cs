using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskerAgent.App.Persistence.Repositories
{
    public interface IDbRepository<T>
    {
        Task<IEnumerable<T>> ListAsync();
        Task<bool> AddAsync(T entity);
        Task<T> FindAsync(string entity);
        Task UpdateAsync(T entity);
        Task RemoveAsync(T entity);
    }
}