using MQTTnet;
using MQTTnet.Client;

namespace KEDA_CommonV2.Interfaces.IMqttServices;

public interface IMqttClientAdapter : IAsyncDisposable
{
    bool IsConnected { get; }

    event Func<MqttApplicationMessageReceivedEventArgs, Task> MessageReceived;
    event Func<MqttClientDisconnectedEventArgs, Task> Disconnected;
    event Func<MqttClientConnectedEventArgs, Task> Connected;

    Task ConnectAsync(MqttClientOptions options, CancellationToken token);
    Task SubscribeAsync(string topic, CancellationToken token);
    Task UnsubscribeAsync(string topic, CancellationToken token);
    Task DisconnectAsync(CancellationToken token);
    Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage message, CancellationToken token);
}
