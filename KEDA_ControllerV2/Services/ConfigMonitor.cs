using KEDA_CommonV2.Interfaces;
using KEDA_ControllerV2.Interfaces;

namespace KEDA_ControllerV2.Services;

public class ConfigMonitor : IConfigMonitor
{
    private readonly ILogger<ConfigMonitor> _logger;
    private readonly IProtocolTaskManager _protocolTaskManager;
    private readonly IWriteTaskManager _writeTaskManager;

    public ConfigMonitor(ILogger<ConfigMonitor> logger, IProtocolTaskManager protocolTaskManager, IWriteTaskManager writeTaskManager)
    {
        _logger = logger;
        _protocolTaskManager = protocolTaskManager;
        _writeTaskManager = writeTaskManager;
    }

    public async Task MonitorAsync(CancellationToken stoppingToken)
    {
        await _writeTaskManager.StartConsumerAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await _protocolTaskManager.RestartAllProtocolsAsync(stoppingToken))
                break;
            _logger.LogError("协议采集任务初始化失败，10秒后重试...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}