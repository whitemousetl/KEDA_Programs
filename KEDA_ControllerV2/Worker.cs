using KEDA_ControllerV2.Interfaces;

namespace KEDA_ControllerV2;

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

        // 订阅 "配置下发主题" 和 "写主题"，都是固定主题。
        var mqttSubscribeManager = scope.ServiceProvider.GetRequiredService<IMqttSubscribeManager>();
        await mqttSubscribeManager.SubscribeConfigAndWriteTopicsAsync(stoppingToken);

        // 主循环,功能：监控配置是否发生改变，改变了重新初始化协议读取
        var configMonitor = scope.ServiceProvider.GetRequiredService<IConfigMonitor>();
        await configMonitor.MonitorAsync(stoppingToken);

        //_logger.LogInformation("后台数据处理服务已停止");
    }
}