using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace KEDA_Controller.Services;
public class MqttSubscribeService : IMqttSubscribeService
{
    private readonly ILogger<MqttSubscribeService> _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    public MqttSubscribeService(ILogger<MqttSubscribeService> logger, IConfiguration config)
    {
        _logger = logger;
        _server = config.GetValue("Mqtt:Server", "localhost") ?? "localhost";
        _port = config.GetValue("Mqtt:Port", 1883);
        _username = config.GetValue("Mqtt:Username", "USER001") ?? "";
        _password = config.GetValue("Mqtt:Password", "USER001") ?? "";
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(_server, _port)
            .WithCredentials(_username, _password)
            .Build();
    }

    public async Task StartAsync<T>(Dictionary<string, Func<T, CancellationToken, Task>> topicHandles, CancellationToken token)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            if (topicHandles.TryGetValue(e.ApplicationMessage.Topic, out var handler))
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    var obj = JsonSerializer.Deserialize<T>(payload);
                    if (obj != null)
                    {
                        _logger.LogInformation("收到MQTT主题[{topic}]消息: {payload}", e.ApplicationMessage.Topic, payload);
                        await handler(obj, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT主题[{topic}]消息处理异常", e.ApplicationMessage.Topic);
                }
            }
        };

        if (!_client.IsConnected)
            await _client.ConnectAsync(_options, token);

        foreach (var topic in topicHandles.Keys)
        {
            await _client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, token);
            _logger.LogInformation("已订阅MQTT主题: {topic}", topic);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
