namespace KEDA_CommonV2.Interfaces;

public interface IMqttPublishService
{
    Task<bool> PublishAsync(string topic, byte[] payload, CancellationToken token);

    Task<bool> PublishAsync(string topic, string payload, CancellationToken token);
}