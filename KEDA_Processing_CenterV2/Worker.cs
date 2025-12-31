using KEDA_Processing_CenterV2.Interfaces;

namespace KEDA_Processing_CenterV2;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("后台数据处理服务已启动");

        using var scope = _scopeFactory.CreateScope();

        // 持续尝试初始化，直到成功或取消,功能：订阅原始数据处理后发布
        var mqttSubscribeManager = scope.ServiceProvider.GetRequiredService<IMqttSubscribeManager>();
        // 主循环,功能：监控配置是否发生改变，改变了重新初始化
        var configMonitor = scope.ServiceProvider.GetRequiredService<IConfigMonitor>();

        await mqttSubscribeManager.InitialAsync(stoppingToken);
        await configMonitor.MonitorAsync(stoppingToken);

        _logger.LogInformation("后台数据处理服务已停止");
    }
}
