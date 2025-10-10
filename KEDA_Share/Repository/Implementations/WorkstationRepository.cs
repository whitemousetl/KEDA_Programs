using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;

namespace KEDA_Share.Repository.Implementations;

public class WorkstationRepository : IWorkstationRepository
{
    private readonly IMongoDbContext<Workstation> _context;

    public WorkstationRepository(IMongoDbContext<Workstation> context)
    {
        _context = context;
    }

    public async Task AddAsync(Workstation entity, CancellationToken ct = default) => await _context.InsertAsync(entity, ct);

    public async Task<Workstation?> GetLatestByTimestampAsync(CancellationToken ct = default) => await _context.FindLatestByAsync("Timestamp", ct);

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

    public Task<bool> UpsertAsync(Workstation entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    #endregion 暂时不需要实现
}