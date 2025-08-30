using KEDA_Share.Entity;

namespace KEDA_Share.Repository.Interfaces;

public interface IWorkstationRepository : IRepository<Workstation, string>
{
    Task<Workstation?> GetLatestByTimestampAsync(CancellationToken ct = default);
}