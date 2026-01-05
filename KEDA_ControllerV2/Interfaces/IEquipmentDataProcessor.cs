using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations.Protocols;
using System.Collections.Concurrent;

namespace KEDA_ControllerV2.Interfaces;

public interface IEquipmentDataProcessor
{
    ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, ProtocolDto protocol, CancellationToken token);
}