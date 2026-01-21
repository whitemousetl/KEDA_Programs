namespace KEDA_CommonV2.Interfaces.IMqttServices;

public interface IMqttPublishService
{
    Task<bool> PublishAsync(string topic, string payload, CancellationToken token);
}

