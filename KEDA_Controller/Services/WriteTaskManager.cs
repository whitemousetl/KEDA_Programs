using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KEDA_Controller.Services;
public class WriteTaskManager : IWriteTaskManager
{
    private readonly Channel<WriteTaskEntity> _writeChannel = Channel.CreateUnbounded<WriteTaskEntity>();//写任务通道
    private readonly ILogger<WriteTaskManager> _logger;
    private readonly IProtocolConfigProvider _configProvider;
    private readonly IProtocolTaskManager _protocolTaskManager;
    private readonly IMqttSubscribeService _mqttSubscribeService;

    public WriteTaskManager(ILogger<WriteTaskManager> logger, IProtocolConfigProvider configProvider, IProtocolTaskManager protocolTaskManager, IMqttSubscribeService mqttSubscribeService)
    {
        _logger = logger;
        _configProvider = configProvider;
        _protocolTaskManager = protocolTaskManager;
        _mqttSubscribeService = mqttSubscribeService;
    }

    public async Task InitSubscribeAsync(CancellationToken token)
    {
        var topicHandles = new Dictionary<string, Func<WriteTaskEntity, CancellationToken, Task>>();
        topicHandles["protocol/write"] = TriggerWriteTaskAsync;
        await _mqttSubscribeService.StartAsync(topicHandles, token);
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
            await HandleProtocolWriteTaskAsync(writeTask, stoppingToken);
        }
    }

    /// <summary>
    /// 写任务处理
    /// 1. 停止目标协议采集任务
    /// 2. 执行写操作
    /// 3. 查询最新配置，重启采集任务
    /// </summary>
    private async Task HandleProtocolWriteTaskAsync(WriteTaskEntity writeTask, CancellationToken stoppingToken)  //写任务处理
    {
        var protocolId = writeTask.ProtocolID;

        await _protocolTaskManager.StopProtocolAsync(protocolId, stoppingToken);

        //执行写任务
        await DoProtocolWriteTaskAsync(protocolId, writeTask, stoppingToken);

        //写完后重启该协议的读取任务
       var protocolEntity = await _configProvider.GetProtocolEntityByProtocolIdAsync(protocolId, stoppingToken);    
        if(protocolEntity != null)
            await _protocolTaskManager.RestartProtocolAsync(protocolId, protocolEntity, stoppingToken);
        else
            _logger.LogWarning($"未找到ProtocolId={protocolId}对应的协议实体，无法重启采集任务。");
    }

    /// <summary>
    /// 协议写操作
    /// 优先使用已存在的驱动对象，若无则新建驱动
    /// </summary>
    private async Task DoProtocolWriteTaskAsync(string protocolId, WriteTaskEntity writeTask, CancellationToken stoppingToken)
    {
        var drivers = _protocolTaskManager.GetDrivers();
        if (drivers.TryGetValue(protocolId, out var driver))
            await driver.WriteAsync(writeTask, stoppingToken);
        else
        {
            var driverNew = ProtocolDriverFactory.CreateDriver(writeTask.ProtocolType);
            if (driverNew == null)
            {
                _logger.LogWarning($"协议驱动未实现: {writeTask.ProtocolType}");
                return;
            }

            try
            {
                await driverNew.WriteAsync(writeTask, stoppingToken);
            }
            finally
            {
                (driverNew as IDisposable)?.Dispose();
            }
        }
    }

    public Task StartConsumerAsync(CancellationToken token)
    {
        _ = WriteTaskConsumerAsync(token);
        return Task.CompletedTask;
    }

    public async Task TriggerWriteTaskAsync(WriteTaskEntity writeTask, CancellationToken token)
    {
        await _writeChannel.Writer.WriteAsync(writeTask, token);
    }
}
