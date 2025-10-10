using KEDA_Share.Repository.Interfaces;
using MongoDB.Driver;

namespace KEDA_Share.Repository.Mongo;

public class MongoDbContext<T> : IMongoDbContext<T>
{
    private readonly IMongoCollection<T> _collection;
    private readonly IMongoDatabase _database;
    public IMongoCollection<T> Collection => _collection;
    public IMongoDatabase Database => _database;

    public MongoDbContext(IMongoCollection<T> collection)
    {
        _collection = collection;
        _database = collection.Database;
    }

    public Task InsertAsync(T entity, CancellationToken ct = default)
    {
        return _collection.InsertOneAsync(entity, null, ct);
    }

    public async Task<T?> FindLatestByAsync(string fieldName, CancellationToken ct = default)
    {
        try
        {
            var sort = Builders<T>.Sort.Descending(fieldName);
            var projection = Builders<T>.Projection.Exclude("_id");
            return await _collection
                .Find(FilterDefinition<T>.Empty)
                .Sort(sort)
                .Project<T>(projection)
                .FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {

            throw;
        }
    }
}