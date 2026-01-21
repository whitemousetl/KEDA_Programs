using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Interfaces.IMqttServices;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace KEDA_CommonV2.Services.MqttServices;

public class MqttPublishService : IMqttPublishService, IAsyncDisposable
{
    private readonly ILogger<MqttPublishService> _logger;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private int _maxRetries;
    private bool _isDisposed;
    private readonly ISharedConfigHelper _sharedConfigHelper;
    private readonly IMqttClientAdapter _clientAdapter;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayFunc;

    public MqttPublishService(ILogger<MqttPublishService> logger, ISharedConfigHelper sharedConfigHelper, IMqttClientAdapter clientAdapter, int maxRetries = 10,
        Func<TimeSpan, CancellationToken, Task>? delayFunc = null)
    {
        _logger = logger;
        _sharedConfigHelper = sharedConfigHelper;
        _clientAdapter = clientAdapter;
        _maxRetries = maxRetries;
        _delayFunc = delayFunc ?? ((ts, ct) => Task.Delay(ts, ct));
    }

    public async Task<bool> PublishAsync(string topic, string payload, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        // 立即检查是否已被 Dispose，避免进入已释放的 semaphore
        if (_isDisposed) return false;

        try
        {
            await _publishLock.WaitAsync(token); // 锁住，限制并发发布，只能串行发布
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        try
        {
            var connected = await EnsureConnectedAsync(token);
            if (!connected)
            {
                _logger.LogWarning("无法建立 MQTT 连接，放弃发布到 {Topic}", topic);
                return false;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            var result = await _clientAdapter.PublishAsync(message, token);
            if (result.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                _logger.LogInformation(message: "已发布数据到 MQTT topic {Topic}", topic);
                return true;
            }

            _logger.LogWarning("MQTT 发布返回非成功状态: {ReasonCode}", result.ReasonCode);
            return false;
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

    private async Task<bool> EnsureConnectedAsync(CancellationToken token)
    {
        if (_isDisposed) return false;
        if (_clientAdapter.IsConnected) return true;

        await _connectionLock.WaitAsync(token);
        try
        {
            // 双重检查
            if (_clientAdapter.IsConnected) return true;

            for (int retry = 0; retry < _maxRetries; retry++)
            {
                try
                {
                    await _clientAdapter.ConnectAsync(_sharedConfigHelper.MqttClientOptions, token);
                    _logger.LogInformation("MQTT 连接成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MQTT 连接失败 (尝试 {Retry}/{Max})", retry + 1, _maxRetries);

                    if (retry < _maxRetries - 1)
                    {
                        await _delayFunc(TimeSpan.FromSeconds(2 * (retry + 1)), token);
                    }
                }
            }
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            if (_clientAdapter.IsConnected)
                await _clientAdapter.DisconnectAsync(CancellationToken.None);
        }
        finally
        {
            _clientAdapter?.DisconnectAsync(CancellationToken.None);
            _publishLock?.Dispose();
            _connectionLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}