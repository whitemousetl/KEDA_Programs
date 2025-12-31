using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace KEDA_Common.Services;

public class MqttSubscribeService : IMqttSubscribeService
{
    private readonly ILogger<MqttSubscribeService> _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private Func<MqttApplicationMessageReceivedEventArgs, Task>? _currentHandler;

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
            .WithCleanSession(true) // 关键：避免历史队列
            .Build();
    }

   

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    //public async Task StartAsync<T>(ConcurrentDictionary<string, Func<T, CancellationToken, Task>> topicHandles, CancellationToken token)
    //{
    //    // 移除旧的事件处理器
    //    if (_currentHandler != null)
    //        _client.ApplicationMessageReceivedAsync -= _currentHandler;

    //    // 新的事件处理器
    //    _currentHandler = async e =>
    //    {
    //        if (topicHandles.TryGetValue(e.ApplicationMessage.Topic, out var handler))
    //        {
    //            try
    //            {
    //                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    //                var obj = JsonSerializer.Deserialize<T>(payload);
    //                if (obj != null)
    //                {
    //                    _logger.LogInformation("收到MQTT主题[{topic}]消息: {payload}", e.ApplicationMessage.Topic, payload);
    //                    await handler(obj, token);//订阅事件触发后，执行清洗转换处理发布到MQTT的方法,handler就是这个方法， DataProcessingWorker.ProcessDataAsync
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                _logger.LogError(ex, "MQTT主题[{topic}]消息处理异常", e.ApplicationMessage.Topic);
    //            }
    //        }
    //    };

    //    _client.ApplicationMessageReceivedAsync += _currentHandler;

    //    await EnsureConnectedAsync(token);

    //    foreach (var topic in topicHandles.Keys)
    //    {
    //        await _client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, token);
    //        _logger.LogInformation("已订阅MQTT主题: {topic}", topic);
    //    }
    //}

    public async Task StartAsync<T>(ConcurrentDictionary<string, Func<T, CancellationToken, Task>> topicHandles, CancellationToken token)
    {
        if (_currentHandler != null)//移除当前事件处理器
            _client.ApplicationMessageReceivedAsync -= _currentHandler;

        // 注册新的事件处理器
        _currentHandler = async e =>
        {
            if (topicHandles.TryGetValue(e.ApplicationMessage.Topic, out var handler))
            {
                try
                {
                    if (e.ApplicationMessage.Retain)
                    {
                        _logger.LogDebug("忽略保留消息: {topic}", e.ApplicationMessage.Topic);
                        return;
                    }

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

        // 注册事件处理器
        _client.ApplicationMessageReceivedAsync += _currentHandler;

        await EnsureConnectedAsync(token);

        foreach (var topic in topicHandles.Keys)
        {
            await _client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, token);
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
}
