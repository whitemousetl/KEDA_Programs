using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_Processing_CenterV2.Interfaces;
public interface IMqttPublishManager
{
    Task ProcessDataAsync(ProtocolResult protocolResult, ProtocolDto protocol, CancellationToken token);
    Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token);
}
