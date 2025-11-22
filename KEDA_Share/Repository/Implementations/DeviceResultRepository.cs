using KEDA_Share.Entity;
using KEDA_Share.Enums;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Implementations;
public class DeviceResultRepository : IDeviceResultRepository
{
    private readonly IMongoDatabase _database;

    // 按设备分隔缓存，线程安全
    private readonly ConcurrentDictionary<string, Queue<DeviceResult>> _deviceCache = [];

    public DeviceResultRepository(IMongoDbContext<DeviceStatus> context)
    {
        // 强制使用名为 collector 的数据库
        _database = context is MongoDbContext<DeviceStatus> mongoContext
            ? mongoContext.Database.Client.GetDatabase("collector")
            : throw new InvalidOperationException("MongoDbContext 不支持直接访问数据库。");
    }

    //public async Task AddAsync(DeviceResult entity, CancellationToken ct = default)
    //{
    //    if (entity.DevId == null || entity.PointResults == null)
    //        throw new ArgumentException("DevId 或 PointResults 不能为空");

    //    // 构建BsonDocument
    //    var doc = new BsonDocument();

    //    foreach (var point in entity.PointResults)
    //    {
    //        if (point.Label == null) continue;
    //        object? value = point.Result;
    //        // 判断是否为表达式转换结果（即 Result 为 double 且 DataType 不是 double/float）
    //        if (value is double doubleResult && point.DataType is not DataType.Double and not DataType.Float)
    //        {
    //            // 强制存为 double，保证精度
    //            doc[point.Label] = BsonValue.Create(doubleResult);
    //        }
    //        else
    //        {
    //            try
    //            {
    //                value = point.DataType switch
    //                {
    //                    DataType.Bool => Convert.ToBoolean(point.Result),
    //                    DataType.UShort => Convert.ToUInt16(point.Result),
    //                    DataType.Short => Convert.ToInt16(point.Result),
    //                    DataType.UInt => Convert.ToUInt32(point.Result),
    //                    DataType.Int => Convert.ToInt32(point.Result),
    //                    DataType.Float => Convert.ToDouble(point.Result),
    //                    DataType.Double => Convert.ToDouble(point.Result),
    //                    DataType.String => Convert.ToString(point.Result),
    //                    _ => point.Result
    //                };
    //            }
    //            catch
    //            {
    //                value = null;
    //            }
    //            doc[point.Label] = value == null ? BsonNull.Value : BsonValue.Create(value);
    //        }
    //    }

    //    var now = DateTime.Now;
    //    var nowOffset = new DateTimeOffset(now);
    //    doc["time"] = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    //    doc["timestamp"] = new BsonInt64(nowOffset.ToUnixTimeMilliseconds()); // 精确到毫秒
    //    doc["expireAt"] = now.AddDays(30); // 30天后过期，类型为DateTime


    //    var fieldCount = doc.ElementCount;
    //    if (fieldCount == 3 &&
    //        doc.Contains("time") &&
    //        doc.Contains("timestamp") &&
    //        doc.Contains("expireAt"))
    //    {
    //        // 只有time、timestamp、expireAt，不保存
    //        return;
    //    }

    //    // 获取集合（集合名为 DevId）
    //    var collection = _database.GetCollection<BsonDocument>(entity.DevId);

    //    // 保证索引
    //    await EnsureIndexesAsync(collection);

    //    // 插入文档
    //    await collection.InsertOneAsync(doc, null, ct);
    //}

    public async Task AddAsync(DeviceResult entity, CancellationToken ct = default)
    {
        if (entity.DevId == null || entity.PointResults == null)
            throw new ArgumentException("DevId 或 PointResults 不能为空");

        // 获取设备对应的缓存队列
        var cache = _deviceCache.GetOrAdd(entity.DevId, _ => new Queue<DeviceResult>());
        lock (cache)
        {
            cache.Enqueue(entity);
            if (cache.Count > 3)
                cache.Dequeue();
        }

        var doc = new BsonDocument();

        foreach (var point in entity.PointResults)
        {
            if (point.Label == null) continue;
            object? value = point.Result;

            // Label值为空时，查找该设备的缓存
            if (value == null)
            {
                lock (cache)
                {
                    foreach (var cachedEntity in cache.Reverse())
                    {
                        var cachedPoint = cachedEntity.PointResults?.FirstOrDefault(p => p.Label == point.Label);
                        if (cachedPoint?.Result != null)
                        {
                            value = cachedPoint.Result;
                            break;
                        }
                    }
                }
            }

            // 判断是否为表达式转换结果（即 Result 为 double 且 DataType 不是 double/float）
            if (value is double doubleResult && point.DataType is not DataType.Double and not DataType.Float)
            {
                doc[point.Label] = BsonValue.Create(doubleResult);
            }
            else
            {
                try
                {
                    value = point.DataType switch
                    {
                        DataType.Bool => Convert.ToBoolean(value),
                        DataType.UShort => Convert.ToUInt16(value),
                        DataType.Short => Convert.ToInt16(value),
                        DataType.UInt => Convert.ToUInt32(value),
                        DataType.Int => Convert.ToInt32(value),
                        DataType.Float => Convert.ToDouble(value),
                        DataType.Double => Convert.ToDouble(value),
                        DataType.String => Convert.ToString(value),
                        _ => value
                    };
                }
                catch
                {
                    value = null;
                }
                doc[point.Label] = value == null ? BsonNull.Value : BsonValue.Create(value);
            }
        }

        var now = DateTime.Now;
        var nowOffset = new DateTimeOffset(now);
        doc["time"] = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        doc["timestamp"] = new BsonInt64(nowOffset.ToUnixTimeMilliseconds());
        doc["expireAt"] = now.AddDays(30);

        var fieldCount = doc.ElementCount;
        if (fieldCount == 3 &&
            doc.Contains("time") &&
            doc.Contains("timestamp") &&
            doc.Contains("expireAt"))
        {
            return;
        }

        var collection = _database.GetCollection<BsonDocument>(entity.DevId);
        await EnsureIndexesAsync(collection);
        await collection.InsertOneAsync(doc, null, ct);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<DeviceResult?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<DeviceResult>> ListAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(DeviceResult entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpsertAsync(DeviceResult entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private static async Task EnsureIndexesAsync(IMongoCollection<BsonDocument> collection)
    {
        // time、timestamp索引
        var timeIndexKeys = Builders<BsonDocument>.IndexKeys.Descending("time");
        var timestampIndexKeys = Builders<BsonDocument>.IndexKeys.Descending("timestamp");
        var expireAtIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("expireAt");

        var timeIndexModel = new CreateIndexModel<BsonDocument>(timeIndexKeys);
        var timestampIndexModel = new CreateIndexModel<BsonDocument>(timestampIndexKeys);
        var expireAtIndexModel = new CreateIndexModel<BsonDocument>(
            expireAtIndexKeys,
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }
        );

        // 获取现有索引列表
        var existingIndexes = await collection.Indexes.ListAsync();
        var indexList = await existingIndexes.ToListAsync();

        var timeIndexExists = indexList.Any(index => index["name"] == "time_-1");
        var timestampIndexExists = indexList.Any(index => index["name"] == "timestamp_-1");
        var expireAtIndexExists = indexList.Any(index => index["name"] == "expireAt_1");

        if (!timeIndexExists)
            await collection.Indexes.CreateOneAsync(timeIndexModel);

        if (!timestampIndexExists)
            await collection.Indexes.CreateOneAsync(timestampIndexModel);

        if (!expireAtIndexExists)
            await collection.Indexes.CreateOneAsync(expireAtIndexModel);
    }
}
