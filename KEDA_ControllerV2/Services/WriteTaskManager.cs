using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace KEDA_ControllerV2.Services;

public class WriteTaskManager : IWriteTaskManager
{
    private readonly Channel<WriteTask> _writeChannel = Channel.CreateUnbounded<WriteTask>();//写任务通道
    private readonly ILogger<WriteTaskManager> _logger;
    private readonly IWorkstationConfigProvider _workstationConfigProvider;
    private readonly IProtocolTaskManager _protocolTaskManager;
    private readonly IWriteTaskLogService _writeTaskLogService;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly JsonSerializerOptions _jsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly HashSet<ProtocolType> _serialProtocols; //串口或类串口协议

    public WriteTaskManager(ILogger<WriteTaskManager> logger, IWorkstationConfigProvider workstationConfigProvider, IProtocolTaskManager protocolTaskManager, IWriteTaskLogService writeTaskLogService, IMqttPublishService mqttPublishService, IConfiguration configuration)
    {
        _logger = logger;
        _workstationConfigProvider = workstationConfigProvider;
        _protocolTaskManager = protocolTaskManager;
        _writeTaskLogService = writeTaskLogService;
        _mqttPublishService = mqttPublishService;
        _serialProtocols = configuration
            .GetSection("SerialProtocol")
            .Get<ConcurrentBag<string>>()?
            .Select(x => Enum.TryParse<ProtocolType>(x, out var pt) ? pt : (ProtocolType?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet() ?? [];
    }

    /// <summary>
    /// 写任务事件消费者
    /// Channel事件驱动，实时响应写任务，保证写优先
    /// </summary>
    private async Task WriteTaskConsumerAsync(CancellationToken stoppingToken)  //写任务事件消费者
    {
        //从队列中获取写任务,Channel 是事件驱动的，只要有写任务被推送（如通过 await _writeChannel.Writer.WriteAsync(...)），消费者会立即处理。
        await foreach (var writeTask in _writeChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleProtocolWriteTaskAsync(writeTask, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写任务处理异常，已忽略，继续处理后续任务");
                // 可选：写任务失败后是否需要重试或通知
            }
        }
    }

    /// <summary>
    /// 写任务处理
    /// 1. 停止目标协议采集任务
    /// 2. 执行写操作
    /// 3. 查询最新配置，重启采集任务
    /// </summary>
    private async Task HandleProtocolWriteTaskAsync(WriteTask writeTask, CancellationToken stoppingToken) // 要修改DoProtocolWriteTaskAsync方法，返回true
    {
        var protocolId = writeTask.Protocol.Id;
        bool isSuccess = false;
        string msg = string.Empty;

        // 判断是否为串口串行协议
        bool isSerial = _serialProtocols.Contains(writeTask.Protocol.ProtocolType);

        if (isSerial)
            await _protocolTaskManager.StopProtocolAsync(protocolId, stoppingToken);

        try
        {
            isSuccess = await DoProtocolWriteTaskAsync(protocolId, writeTask, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写任务异常");
            msg = ex.Message;
            isSuccess = false; // 有异常，视为失败
        }
        finally
        {
            var log = new WriteTaskLog
            {
                UUID = writeTask.UUID,
                EquipmentType = writeTask.Protocol.Equipments[0].EquipmentType,
                WriteTaskJson = JsonSerializer.Serialize(writeTask, _jsonOptions),
                Time = DateTime.UtcNow,
                TimeLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsSuccess = isSuccess,
                Msg = msg,
            };
            await _writeTaskLogService.AddLogAsync(log);

            // 发布写任务结果到MQTT
            var resultTopic = SharedConfigHelper.MqttTopicSettings.ProtocolWriteResultPrefix;
            var resultPayload = JsonSerializer.Serialize(log, _jsonOptions);
            await _mqttPublishService.PublishAsync(resultTopic, resultPayload, stoppingToken);

            if (isSerial)
            {
                var protocolEntity = await _workstationConfigProvider.GetProtocolByProtocolIdAsync(protocolId, stoppingToken);
                if (protocolEntity != null)
                    await _protocolTaskManager.RestartProtocolAsync(protocolId, protocolEntity, stoppingToken);
                else
                    _logger.LogWarning($"未找到ProtocolId={protocolId}对应的协议实体，无法重启采集任务。");
            }
        }
    }

    private readonly ConcurrentDictionary<string, IProtocolDriver> _tcpDriverCache = new();

    /// <summary>
    /// 协议写操作
    /// 优先使用已存在的驱动对象，若无则新建驱动，执行完写任务就释放连接，特殊协议即使驱动不存在也不会执行完写任务就释放连接
    /// </summary>
    private async Task<bool> DoProtocolWriteTaskAsync(string protocolId, WriteTask writeTask, CancellationToken stoppingToken)
    {
        // 1. 优先查找全局驱动池
        var drivers = _protocolTaskManager.GetDrivers();
        if (drivers.TryGetValue(protocolId, out var driver))
            return await driver.WriteAsync(writeTask, stoppingToken);

        // 2. 查找本地TCP驱动缓存
        if (_tcpDriverCache.TryGetValue(protocolId, out var cachedDriver))
            return await cachedDriver.WriteAsync(writeTask, stoppingToken);

        // 3. 都没有则新建
        var protocolType = writeTask.Protocol.ProtocolType;
        var driverNew = ProtocolDriverFactory.CreateDriver(protocolType, _mqttPublishService);
        if (driverNew == null)
        {
            var msg = $"协议驱动未实现: {protocolType}";
            _logger.LogWarning(msg);
            throw new NotSupportedException(msg);
        }

        bool isSerial = _serialProtocols.Contains(protocolType);

        if (isSerial)
        {
            // 串口协议：用完即释放
            try
            {
                return await driverNew.WriteAsync(writeTask, stoppingToken);
            }
            finally
            {
                (driverNew as IDisposable)?.Dispose();
            }
        }
        else
        {
            // 非串口协议：加入本地缓存，后续复用
            _tcpDriverCache.TryAdd(protocolId, driverNew);
            return await driverNew.WriteAsync(writeTask, stoppingToken);
        }
    }

    public Task StartConsumerAsync(CancellationToken token)
    {
        _ = WriteTaskConsumerAsync(token);
        _logger.LogInformation("写任务消费者已启动，等待写任务入队...");
        return Task.CompletedTask;
    }

    public async Task TriggerWriteTaskAsync(WriteTask writeTask, CancellationToken token)
    {
        if (writeTask == null)
        {
            _logger.LogWarning("写任务为空，已跳过");
            return;
        }

        await _writeChannel.Writer.WriteAsync(writeTask, token);
    }
}