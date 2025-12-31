using KEDA_CommonV2.Model;
using System.Collections.Concurrent;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IDeviceDataProcessor
{
    ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, Protocol protocol, CancellationToken token);
}
