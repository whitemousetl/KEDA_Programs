using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Interfaces;
public interface IRepository<T, TId> where T : class
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(TId id, CancellationToken ct = default);
}
