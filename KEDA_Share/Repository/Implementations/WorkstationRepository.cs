using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Repository.Implementations;
public class WorkstationRepository : IWorkstationRepository
{
    private readonly IMongoCollection<Workstation> _workstations;

    public WorkstationRepository(IMongoCollection<Workstation> workstations)
    {
        _workstations = workstations;
    }

    public async Task AddAsync(Workstation entity, CancellationToken ct = default)
    {
        await _workstations.InsertOneAsync(entity, null!, ct);
    }

    public async Task<Workstation?> GetLatestByTimestampAsync(CancellationToken ct = default)
    {
        //排除_id字段
        var projection = Builders<Workstation>.Projection.Exclude("_id");

        //按Timestamp字段降序排序，取最新一条
        var workstation = await _workstations
            .Find(FilterDefinition<Workstation>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project<Workstation>(projection)
            .FirstOrDefaultAsync(ct);  

        return workstation;
    }

    #region 暂时不需要实现
    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Workstation?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Workstation>> ListAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(Workstation entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    } 
    #endregion
}
