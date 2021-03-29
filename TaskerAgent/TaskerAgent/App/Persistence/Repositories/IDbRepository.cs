using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskerAgent.App.Persistence.Repositories
{
    public interface IDbRepository<T>
    {
        Task<IEnumerable<string>> ListAsync();
        Task<bool> AddAsync(T entity);
        Task<T> FindAsync(string entity);
        Task<bool> AddOrUpdateAsync(T entity);
        Task RemoveAsync(T entity);
    }
}