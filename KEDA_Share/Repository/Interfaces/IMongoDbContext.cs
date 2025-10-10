namespace KEDA_Share.Repository.Interfaces;

public interface IMongoDbContext<T>
{
    Task InsertAsync(T entity, CancellationToken ct = default);

    Task<T?> FindLatestByAsync(string fieldName, CancellationToken ct = default);
}