using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Model;

namespace KEDA_CommonV2.Interfaces;

public interface IWorkstationConfigProvider
{
    bool IsConfigChanged(WorkstationConfig? latestConfig, string lastConfigTime);

    Task<Protocol?> GetProtocolByProtocolIdAsync(string protocolId, CancellationToken token);

    Task<Protocol?> GetProtocolByDeviceIdAsync(string protocolId, CancellationToken token);

    Task<Workstation?> GetLatestWrokstationAsync(CancellationToken token);

    Task<WorkstationConfig?> GetLatestWorkstationConfigEntityAsync(CancellationToken token);

    Task SaveConfigAsync(WorkstationConfig entity, CancellationToken token);
}