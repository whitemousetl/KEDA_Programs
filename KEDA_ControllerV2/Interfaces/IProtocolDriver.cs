using KEDA_CommonV2.Model;

namespace KEDA_ControllerV2.Interfaces;

public interface IProtocolDriver : IDisposable
{
    Task<PointResult?> ReadAsync(Protocol protocol, string devId, Point point, CancellationToken token);
    Task<ProtocolResult?> ReadAsync(Protocol protocol, CancellationToken token);
    Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token);
    string GetProtocolName();
}