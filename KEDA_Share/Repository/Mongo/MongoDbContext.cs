using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Mongo;
public class MongoDbContext<T> : IMongoDbContext<T>
{
    private readonly IMongoCollection<T> _collection;

    public MongoDbContext(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public async Task<T?> FindLatestByAsync<TKey>(Func<T, TKey> keySelector, CancellationToken ct = default)
    {
        return await _collection
            .Find(FilterDefinition<T>.Empty)
            .SortByDescending(x => keySelector(x))
            .FirstOrDefaultAsync(ct);
    }

    public Task InsertAsync(T entity, CancellationToken ct = default)
    {
        return _collection.InsertOneAsync(entity, null, ct);
    }
}
