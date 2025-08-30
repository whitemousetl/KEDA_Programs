namespace KEDA_Share.Repository.Interfaces;

public interface IMongoDbContext<T>
{
    Task InsertAsync(T entity, CancellationToken ct = default);

    Task<T?> FindLatestByAsync<TKey>(Func<T, TKey> keySelector, CancellationToken ct = default);
}