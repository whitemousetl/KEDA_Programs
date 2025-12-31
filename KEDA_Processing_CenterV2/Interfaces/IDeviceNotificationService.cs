using KEDA_CommonV2.Model;
using System.Collections.Concurrent;

namespace KEDA_Processing_CenterV2.Interfaces;

public interface IDeviceNotificationService
{
    Task MonitorDeviceStatusAsync(ConcurrentBag<ProtocolResult> results, CancellationToken token);
}
