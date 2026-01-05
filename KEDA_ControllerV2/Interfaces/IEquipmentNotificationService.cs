using KEDA_CommonV2.Model;
using System.Collections.Concurrent;

namespace KEDA_ControllerV2.Interfaces;

public interface IEquipmentNotificationService
{
    Task MonitorEquipmentStatusAsync(ProtocolResult result, CancellationToken token);
}