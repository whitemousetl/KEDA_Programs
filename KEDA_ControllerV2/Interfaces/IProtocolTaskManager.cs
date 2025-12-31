using KEDA_CommonV2.Model;
using System.Collections.Concurrent;

namespace KEDA_ControllerV2.Interfaces;

public interface IProtocolTaskManager
{
    Task StopAllAsync(CancellationToken token);

    Task StartAllAsync(CancellationToken token);

    Task<bool> RestartAllProtocolsAsync(CancellationToken token);

    Task StopProtocolAsync(string protocolId, CancellationToken token);

    Task RestartProtocolAsync(string protocolId, Protocol protocol, CancellationToken token);

    ConcurrentDictionary<string, IProtocolDriver> GetDrivers();
}