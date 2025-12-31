using CollectorService.Models;
using KEDA_Share.Entity;

namespace CollectorService.Protocols;
public interface IProtocolDriver : IDisposable
{
    Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token);

    Task<DeviceResult> ReadAsync(Protocol protocol, Device device,  CancellationToken token);
}