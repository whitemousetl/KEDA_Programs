using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_ControllerV2.Interfaces;

public interface IProtocolDriver : IDisposable
{
    Task<PointResult?> ReadAsync(ProtocolDto protocol, string devId, ParameterDto point, CancellationToken token);
    Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token);
    Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token);
    string GetProtocolName();
}