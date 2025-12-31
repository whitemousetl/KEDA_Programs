using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using SqlSugar;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Channels;

namespace KEDA_Controller;
public class Worker : BackgroundService
{
    private readonly IProtocolConfigProvider _configProvider;//获取协议配置的服务
    private readonly IProtocolTaskManager _taskManager;//任务管理服务
    private readonly IWriteTaskManager _writeTaskManager;//写任务管理服务
    private DateTime _lastConfigTime;//配置最新的时间
    private readonly ILogger<Worker> _logger;//日志

    public Worker(IProtocolConfigProvider configProvider, IProtocolTaskManager taskManager, IWriteTaskManager writeTaskManager, ILogger<Worker> logger)
    {
        _configProvider = configProvider;
        _taskManager = taskManager;
        _writeTaskManager = writeTaskManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _writeTaskManager.InitSubscribeAsync(stoppingToken);//初始化MQTT订阅触发时需要执行的逻辑，写任务入列。触发订阅时，生产者会入列
        await Task.Delay(1000, stoppingToken);
        await _writeTaskManager.StartConsumerAsync(stoppingToken);//启动写任务消费，事件驱动，有写任务到Channel，消费者就会立即处理

        while (!stoppingToken.IsCancellationRequested)
        {
            var latestConfig = await _configProvider.GetLatestConfigAsync(stoppingToken);//获取最新的json配置

            if (latestConfig == null)
            {
                _logger.LogWarning("最新协议配置为空,五秒后重试...");
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            if (_configProvider.IsConfigChanged(latestConfig, _lastConfigTime))//如果时间发生更改，则停止所有读任务再执行所有读任务
            {
                _logger.LogInformation("检测到新配置，重启采集任务 ...");
                await _taskManager.StopAllAsync(stoppingToken);
                _lastConfigTime = latestConfig.SaveTime;
                await _taskManager.StartAllAsync(latestConfig, stoppingToken);
            }

            await Task.Delay(5000, stoppingToken);//5秒检查一次配置是否发生更改
        }
    }
}
