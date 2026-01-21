namespace KEDA_CommonV2.Interfaces.IMqttServices;

public interface IMqttMessageHandler
{
    string Topic { get; }

    Task HandleAsync(string payload, CancellationToken cancellationToken);
}