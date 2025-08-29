using KEDA_Share.Entity;
using KEDA_Share.Repository.Mongo;
using Mongo2Go;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Test.Repository.Mongo;
public class MongoDbContextTest : IDisposable
{
    private readonly MongoDbRunner _runner;

    public MongoDbContextTest()
    {
        _runner = MongoDbRunner.Start();
    }

    public void Dispose() => _runner.Dispose();

    [Fact]
    public void GetCollection_ReturnWorkstation()
    {
        var connStr = _runner.ConnectionString;
        var context = new MongoDbContext(connStr);
        var collection = context.GetCollection<Workstation>("TestDb", "Workstation");

        // 插入一条数据
        var workstation = new Workstation { EdgeID = "test", EdgeName = "test", Timestamp = 1, Time = "2024-01-01", Protocols = [] };
        collection.InsertOne(workstation);

        var filter = Builders<Workstation>.Filter.Eq(x => x.EdgeID, "test");
        var projection = Builders<Workstation>.Projection.Exclude("_id");

        var result = collection
            .Find(filter)
            .Project<Workstation>(projection)
            .FirstOrDefault();

        Assert.NotNull(collection);
        Assert.IsType<IMongoCollection<Workstation>>(collection, false);
        Assert.NotNull(result);
        Assert.Equal("test", result.EdgeID);
    }
}
