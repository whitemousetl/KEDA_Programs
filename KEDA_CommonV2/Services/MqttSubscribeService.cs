using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace KEDA_CommonV2.Services;

public class MqttSubscribeService : IMqttSubscribeService
{
    private readonly ILogger<MqttSubscribeService> _logger;
    private readonly string _server;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly int _reconnectDelaySeconds;
    private readonly int _maxReconnectDelaySeconds;
    private readonly int _messageTimeoutSeconds;
    private readonly bool _autoReconnect;
    private readonly IMqttClient _client;
    private readonly MqttTopicSettings _topics;
    private readonly MqttClientOptions _options;
    private readonly ConcurrentDictionary<string, Delegate> _topicHandlers = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private int _reconnectAttempts = 0;
    private bool _isDisposed;

    public MqttSubscribeService(ILogger<MqttSubscribeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 加载配置
        _server = SharedConfigHelper.MqttSettings.Server;
        _port = SharedConfigHelper.MqttSettings.Port;
        _username = SharedConfigHelper.MqttSettings.Username;
        _password = SharedConfigHelper.MqttSettings.Password;
        _reconnectDelaySeconds = SharedConfigHelper.MqttSettings.ReconnectDelaySeconds;
        _maxReconnectDelaySeconds = SharedConfigHelper.MqttSettings.MaxReconnectDelaySeconds;
        _messageTimeoutSeconds = SharedConfigHelper.MqttSettings.MessageTimeoutSeconds;
        _autoReconnect = SharedConfigHelper.MqttSettings.AutoReconnect;
        _topics = SharedConfigHelper.MqttTopicSettings;

        // 参数验证
        if (_reconnectDelaySeconds < 1) _reconnectDelaySeconds = 5;
        if (_maxReconnectDelaySeconds < _reconnectDelaySeconds) _maxReconnectDelaySeconds = _reconnectDelaySeconds;
        if (_messageTimeoutSeconds < 1) _messageTimeoutSeconds = 30;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(_server, _port)
            .WithCredentials(_username, _password)
            .WithCleanSession(true) // 避免历史队列
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithTimeout(TimeSpan.FromSeconds(10))
            .Build();

        // 注册事件处理器
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
        _client.ConnectedAsync += OnConnectedAsync;

        _logger.LogInformation(
            "MQTT订阅服务已初始化 [服务器:  {server}:{port}, 重连延迟: {reconnect}s, 最大重连延迟: {maxReconnect}s, 消息超时: {timeout}s]",
            _server, _port, _reconnectDelaySeconds, _maxReconnectDelaySeconds, _messageTimeoutSeconds);
    }

    /// <summary>
    /// 连接成功事件
    /// </summary>
    private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _reconnectAttempts = 0; // 重置重连计数
        _logger.LogInformation("MQTT客户端已连接到 {server}:{port}", _server, _port);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 断开连接事件 - 自动重连
    /// </summary>
    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        if (_isDisposed || !_autoReconnect)
        {
            _logger.LogInformation("MQTT客户端已断开连接，不进行重连");
            return;
        }

        _logger.LogWarning("MQTT客户端断开连接:  {reason}，准备自动重连.. .", e.Reason);

        // 使用指数退避策略计算延迟时间
        var delay = CalculateReconnectDelay();
        _logger.LogInformation("将在 {delay} 秒后尝试第 {attempt} 次重连", delay, _reconnectAttempts + 1);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delay), _disposeCts.Token);

            if (!_isDisposed)
            {
                await EnsureConnectedAsync(_disposeCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("重连任务已取消");
        }
    }

    /// <summary>
    /// 计算重连延迟时间（指数退避策略）
    /// </summary>
    private int CalculateReconnectDelay()
    {
        _reconnectAttempts++;

        // 指数退避:  delay = min(initialDelay * 2^(attempts-1), maxDelay)
        var exponentialDelay = _reconnectDelaySeconds * Math.Pow(2, _reconnectAttempts - 1);
        var delay = (int)Math.Min(exponentialDelay, _maxReconnectDelaySeconds);

        return delay;
    }

    /// <summary>
    /// 消息接收处理
    /// </summary>
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        if (_isDisposed) return;

        var topic = e.ApplicationMessage.Topic;

        try
        {
            // 忽略保留消息
            if (e.ApplicationMessage.Retain)
            {
                _logger.LogDebug("忽略保留消息: {topic}", topic);
                return;
            }

            if (!_topicHandlers.TryGetValue(topic, out var handler))
            {
                _logger.LogDebug("未找到主题 [{topic}] 的处理器", topic);
                return;
            }

            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("收到空消息: {topic}", topic);
                return;
            }

            _logger.LogDebug("收到MQTT消息 [{topic}]:  {payload}", topic, payload);

            // 使用超时机制处理消息
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(_messageTimeoutSeconds));

            await ProcessMessageAsync(handler, topic, payload, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MQTT主题 [{topic}] 消息处理超时 ({timeout}秒)", topic, _messageTimeoutSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT主题 [{topic}] 消息处理异常", topic);
        }
    }

    /// <summary>
    /// 处理消息并分发到对应的处理器
    /// </summary>
    private async Task ProcessMessageAsync(Delegate handler, string topic, string payload, CancellationToken cancellationToken)
    {
        try
        {
            // 根据处理器类型进行分发
            if (topic == _topics.WorkstationConfigSendPrefix && handler is Func<string, CancellationToken, Task> configHandler)
                await configHandler(payload, cancellationToken);
            else if (topic == _topics.ProtocolWritePrefix && handler is Func<string, CancellationToken, Task> writeHandler)
                await writeHandler(payload, cancellationToken);
            else
                _logger.LogError("未知的处理器类型:  {type}", handler.GetType().Name);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON反序列化失败 [{topic}]:  {payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息处理器执行失败:  {topic}", topic);
            throw; // 重新抛出以便外层捕获
        }
    }

    /// <summary>
    /// 添加主题订阅
    /// </summary>
    public async Task AddSubscribeTopicAsync<T>(string topicName, Func<T, CancellationToken, Task> handler, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("主题名称不能为空", nameof(topicName));

        ArgumentNullException.ThrowIfNull(handler);

        ThrowIfDisposed();

        _topicHandlers[topicName] = handler;

        await EnsureConnectedAsync(token);

        await _client.SubscribeAsync(topicName, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, token);

        _logger.LogInformation("已订阅MQTT主题:  {topic} (类型: {type})", topicName, typeof(string).Name);
    }

    /// <summary>
    /// 移除主题订阅
    /// </summary>
    public async Task RemoveSubscribeTopicAsync(string topicName, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("主题名称不能为空", nameof(topicName));

        ThrowIfDisposed();

        if (_topicHandlers.TryRemove(topicName, out _))
        {
            if (_client.IsConnected)
            {
                await _client.UnsubscribeAsync(topicName, token);
                _logger.LogInformation("已取消订阅MQTT主题:  {topic}", topicName);
            }
            else
            {
                _logger.LogWarning("客户端未连接，仅移除本地订阅记录:  {topic}", topicName);
            }
        }
        else
        {
            _logger.LogWarning("尝试取消订阅不存在的主题: {topic}", topicName);
        }
    }

    /// <summary>
    /// 确保客户端已连接（线程安全）
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken token)
    {
        if (_client.IsConnected) return;

        await _connectionLock.WaitAsync(token);
        try
        {
            // 双重检查锁定
            if (_client.IsConnected) return;

            while (!_client.IsConnected && !token.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    _logger.LogInformation("正在连接到MQTT服务器 {server}:{port}...", _server, _port);
                    await _client.ConnectAsync(_options, token);
                    _logger.LogInformation("MQTT连接成功");

                    // 重新订阅所有主题
                    await ResubscribeAllTopicsAsync(token);
                    break;
                }
                catch (Exception ex)
                {
                    var delay = CalculateReconnectDelay();
                    _logger.LogWarning(ex, "MQTT连接失败，{delay}秒后进行第 {attempt} 次重试.. .",
                        delay, _reconnectAttempts);

                    await Task.Delay(TimeSpan.FromSeconds(delay), token);
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 重新订阅所有主题（用于重连后）
    /// </summary>
    private async Task ResubscribeAllTopicsAsync(CancellationToken token)
    {
        if (_topicHandlers.IsEmpty)
        {
            _logger.LogDebug("没有需要重新订阅的主题");
            return;
        }

        _logger.LogInformation("重新订阅 {count} 个主题.. .", _topicHandlers.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var kvp in _topicHandlers)
        {
            try
            {
                await _client.SubscribeAsync(kvp.Key, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, token);
                _logger.LogDebug("重新订阅主题成功: {topic}", kvp.Key);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新订阅主题失败: {topic}", kvp.Key);
                failCount++;
            }
        }

        _logger.LogInformation("主题重新订阅完成: 成功 {success} 个，失败 {fail} 个", successCount, failCount);
    }

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(MqttSubscribeService));
        }
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        _logger.LogInformation("正在关闭MQTT订阅服务...");

        try
        {
            // 取消所有后台操作
            _disposeCts.Cancel();

            // 取消订阅所有主题
            if (_client.IsConnected && !_topicHandlers.IsEmpty)
            {
                _logger.LogInformation("取消订阅 {count} 个主题...", _topicHandlers.Count);

                foreach (var topic in _topicHandlers.Keys)
                {
                    try
                    {
                        await _client.UnsubscribeAsync(topic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "取消订阅主题失败: {topic}", topic);
                    }
                }
            }

            _topicHandlers.Clear();

            // 断开连接
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
                _logger.LogInformation("MQTT客户端已断开连接");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭MQTT订阅服务时发生异常");
        }
        finally
        {
            // 释放资源
            _client?.Dispose();
            _connectionLock?.Dispose();
            _disposeCts?.Dispose();

            _logger.LogInformation("MQTT订阅服务已关闭");
        }

        GC.SuppressFinalize(this);
    }
}