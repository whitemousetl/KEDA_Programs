using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_ControllerV2.Interfaces;

public interface IMqttPublishManager
{
    Task ProcessDataAsync(ProtocolResult protocolResult, ProtocolDto protocol, CancellationToken token);

    Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token);
}