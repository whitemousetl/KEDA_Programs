using KEDA_Common.Interfaces;
using MQTTnet.Client;
using MQTTnet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KEDA_Common.Services;

public class MqttPublishService : IMqttPublishService, IAsyncDisposable
{
    private readonly ILogger<MqttPublishService> _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public MqttPublishService(ILogger<MqttPublishService> logger, IConfiguration config)
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

    public async Task<bool> PublishAsync(string topic, string payload, CancellationToken token)
    {
        await _publishLock.WaitAsync(token);//锁住，限制并发发布，只能串行发布
        try
        {
            await EnsureConnectedAsync(token);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build();

            await _client.PublishAsync(message, token);
            _logger.LogInformation("已发布数据到 MQTT: {data}", payload);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT 发布失败");
            return false;
        }
        finally
        {
            _publishLock.Release();
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

    public async Task<bool> PublishAsync(string topic, byte[] payload, CancellationToken token)
    {
        await _publishLock.WaitAsync(token);//锁住，限制并发发布，只能串行发布
        try
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_options, token);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build();

            await _client.PublishAsync(message, token);
            _logger.LogInformation("已发布数据到 MQTT: {data}", payload);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT 发布失败");
            return false;
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client?.Dispose();
        _publishLock?.Dispose();
        GC.SuppressFinalize(this);
    }
}
