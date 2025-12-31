namespace KEDA_ControllerV2.Interfaces;

public interface IMqttSubscribeManager
{
    Task<bool> SubscribeConfigAndWriteTopicsAsync(CancellationToken stoppingToken);
}