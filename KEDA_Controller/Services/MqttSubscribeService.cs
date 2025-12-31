using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;
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

    public async Task StartAsync<T>(ConcurrentDictionary<string, Func<T, CancellationToken, Task>> topicHandles, CancellationToken token)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            if (topicHandles.TryGetValue(e.ApplicationMessage.Topic, out var handler))
            {
                try
                {
                    
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    var obj = JsonSerializer.Deserialize<T>(payload);

                    // 校验UUID（仅对WritePointData类型做校验）
                    if (obj is KEDA_Common.Model.WritePointData writePointData)
                    {
                        if (string.IsNullOrWhiteSpace(writePointData.UUID))
                        {
                            _logger.LogError("收到MQTT写任务数据缺少UUID，已拒绝处理。原始数据: {payload}", payload);
                            return;
                        }
                    }

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

        await EnsureConnectedAsync(token);

        foreach (var topic in topicHandles.Keys)
        {
            await _client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, token);
            _logger.LogInformation("已订阅MQTT主题: {topic}", topic);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken token)
    {
        while (!_client.IsConnected && !token.IsCancellationRequested)
        {
            try
            {
                await _client.ConnectAsync(_options, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT连接失败，5秒后重试...");
                await Task.Delay(5000, token);
            }
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
