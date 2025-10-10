using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Implementations;
public class DeviceStatusRepository : IDeviceStatusRepository
{
    private readonly IMongoDbContext<DeviceStatus> _context;
    private readonly IMongoDatabase _database;

    public DeviceStatusRepository(IMongoDbContext<DeviceStatus> context)
    {
        _context = context;
        // 你需要在 IMongoDbContext<T> 中暴露 IMongoDatabase
        _database = (context as MongoDbContext<DeviceStatus>)?.Database
            ?? throw new InvalidOperationException("MongoDbContext 不支持直接访问数据库。");
    }

    public async Task AddAsync(DeviceStatus entity, CancellationToken ct = default)
    {
        var collection = GetCollection(entity.DeviceId);
        await collection.InsertOneAsync(entity, null, ct);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<DeviceStatus?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<DeviceStatus>> ListAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private IMongoCollection<DeviceStatus> GetCollection(string deviceId)
    {
        // 集合名即为 DeviceId
        return _database.GetCollection<DeviceStatus>(deviceId);
    }

    public Task<bool> UpdateAsync(DeviceStatus entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpsertAsync(DeviceStatus entity, CancellationToken ct = default)
    {
        entity.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // 统一更新时间
        foreach (var p in entity.PointStatuses)
        {
            if(string.IsNullOrWhiteSpace(p.UpdateTime))
                p.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        var collection = GetCollection(entity.DeviceId);
        var filter = Builders<DeviceStatus>.Filter.Eq(x => x.DeviceId, entity.DeviceId);
        var result = await collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, ct);
        return result.ModifiedCount > 0 || result.UpsertedId != null;
    }

    public async Task<List<DeviceStatus>> GetAllLatestDeviceStatusAsync(CancellationToken ct = default)
    {
        var deviceStatusList = new List<DeviceStatus>();
        // 获取所有集合名（即所有设备ID）
        var collectionNames = await _database.ListCollectionNames().ToListAsync(ct);

        foreach (var collectionName in collectionNames)
        {
            var collection = _database.GetCollection<DeviceStatus>(collectionName);
            // 排除_id字段，按更新时间降序，取最新一条
            var projection = Builders<DeviceStatus>.Projection.Exclude("_id");
            var latestStatus = await collection
                .Find(FilterDefinition<DeviceStatus>.Empty)
                .SortByDescending(x => x.UpdateTime)
                .Project<DeviceStatus>(projection)
                .FirstOrDefaultAsync(ct);

            if (latestStatus != null)
            {
                deviceStatusList.Add(latestStatus);
            }
        }
        return deviceStatusList;
    }
}
