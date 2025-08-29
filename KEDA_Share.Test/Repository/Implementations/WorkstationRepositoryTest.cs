using KEDA_Share.Entity;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Mongo;
using Mongo2Go;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Test.Repository.Implementations;
public class WorkstationRepositoryTest : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbContext _context;
    private readonly IMongoCollection<Workstation> _workstation;
    private readonly WorkstationRepository _workstationRepository;

    public WorkstationRepositoryTest()
    {
        _runner = MongoDbRunner.Start();
        _context = new MongoDbContext(_runner.ConnectionString);
        _workstation = _context.GetCollection<Workstation>("StationConfigurarion", "Workstation");
        _workstationRepository = new WorkstationRepository(_workstation);

    }

    public void Dispose() => _runner.Dispose();

    [Fact]
    public async Task AddAsync_ShouldInsertWorkstation()
    {
        var workstation = new Workstation
        {
            EdgeID = "edge1",
            EdgeName = "Edge One",
            Timestamp = 100,
            Time = "2024--01-01",
            Protocols = []
        };

        await _workstationRepository.AddAsync(workstation);

        var filter = Builders<Workstation>.Filter.Eq(x => x.EdgeID, value: "edge1");
        var projection = Builders<Workstation>.Projection.Exclude("_id");
        var result = await _workstation.Find(filter).Project<Workstation>(projection).FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("Edge One", result.EdgeName);
    }

    [Fact]
    public async Task GgetLatestByTimestampAsync_ShouldReturnLatestWorkstation()
    {
        var ws1 = new Workstation { EdgeID = "e1", EdgeName = "A", Timestamp = 1, Time = "2024-01-01", Protocols = [] };
        var ws2 = new Workstation { EdgeID = "e2", EdgeName = "B", Timestamp = 2, Time = "2024-01-02", Protocols = [] };

        await _workstation.InsertManyAsync([ws1, ws2]);

        var latest = await _workstationRepository.GetLatestByTimestampAsync();

        Assert.NotNull(latest);
        Assert.Equal("e2", latest.EdgeID);
        Assert.Equal(2, latest.Timestamp);
    }


}
