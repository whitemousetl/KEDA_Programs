using KEDA_CommonV2.Interfaces.IMqttServices;
using MQTTnet;
using MQTTnet.Client;

namespace KEDA_CommonV2.Services.MqttServices;

public class MqttNetClientAdapter : IMqttClientAdapter
{
    private readonly IMqttClient _client;

    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? MessageReceived;
    public event Func<MqttClientDisconnectedEventArgs, Task>? Disconnected;
    public event Func<MqttClientConnectedEventArgs, Task>? Connected;

    public MqttNetClientAdapter(IMqttClient mqttClient)
    {
        _client = mqttClient;
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            if (MessageReceived != null)
            {
                await MessageReceived.Invoke(e);
            }
        };
        _client.DisconnectedAsync += async e =>
        {
            if (Disconnected != null)
            {
                await Disconnected.Invoke(e);
            }
        };
        _client.ConnectedAsync += async e =>
        {
            if (Connected != null)
            {
                await Connected.Invoke(e);
            }
        };
    }

    public bool IsConnected => _client.IsConnected;

    public Task ConnectAsync(MqttClientOptions options, CancellationToken token) => _client.ConnectAsync(options, token);

    public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage message, CancellationToken token) => _client.PublishAsync(message, token);

    public Task DisconnectAsync(CancellationToken token) => _client.DisconnectAsync(new MqttClientDisconnectOptions(), token);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    public Task SubscribeAsync(string topic, CancellationToken token) => _client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, token);

    public Task UnsubscribeAsync(string topic, CancellationToken token) => _client.UnsubscribeAsync(topic, token);
}