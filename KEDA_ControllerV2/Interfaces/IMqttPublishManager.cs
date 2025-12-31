using KEDA_CommonV2.Model;

namespace KEDA_ControllerV2.Interfaces;

public interface IMqttPublishManager
{
    Task ProcessDataAsync(ProtocolResult protocolResult, Protocol protocol, CancellationToken token);

    Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token);
}