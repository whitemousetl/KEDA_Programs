using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_CommonV2.Interfaces;

public interface IWorkstationConfigProvider
{
    bool IsConfigChanged(WorkstationConfig? latestConfig, string lastConfigTime);

    Task<ProtocolDto?> GetProtocolByProtocolIdAsync(string protocolId, CancellationToken token);

    Task<ProtocolDto?> GetProtocolByDeviceIdAsync(string protocolId, CancellationToken token);

    Task<WorkstationDto?> GetLatestWrokstationAsync(CancellationToken token);

    Task<WorkstationConfig?> GetLatestWorkstationConfigEntityAsync(CancellationToken token);

    Task SaveConfigAsync(WorkstationConfig entity, CancellationToken token);
}