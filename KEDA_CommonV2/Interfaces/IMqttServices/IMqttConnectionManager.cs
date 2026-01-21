namespace KEDA_CommonV2.Interfaces.IMqttServices;

public interface IMqttConnectionManager
{
    Task EnsureConnectedAsync(CancellationToken cancellationToken);

    bool IsReady { get; }

    event Action Connected;

    event Action Disconnected;
}
