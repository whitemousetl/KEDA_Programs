using KEDA_Common.Model;
using System.Collections.Concurrent;

namespace KEDA_Processing_Center.Interfaces;

public interface IDeviceNotificationService
{
    Task MonitorDeviceStatusAsync(ConcurrentBag<ProtocolResult> results, CancellationToken token);
}
