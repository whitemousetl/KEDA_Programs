using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using System.Text.Json;

namespace KEDA_Processing_Center.Services;

public class DataProcessingWorker : BackgroundService
{
    private readonly ILogger<DataProcessingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IMqttSubscribeService _mqttSubscribeService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public DataProcessingWorker(
        ILogger<DataProcessingWorker> logger,
        IServiceProvider serviceProvider,
        IMqttPublishService mqttPublishService,
        IMqttSubscribeService mqttSubscribeService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mqttPublishService = mqttPublishService;
        _mqttSubscribeService = mqttSubscribeService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("后台数据处理服务已启动");

        // 持续尝试初始化，直到成功或取消
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await InitializeConfigurationAndSubscriptions(stoppingToken))
                break; // 初始化成功，跳出重试循环
            _logger.LogError("初始化配置和订阅失败，10秒后重试...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        // 主循环
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // 减少日志频率

                // 可选：定期检查配置更新
                await CheckForConfigurationUpdates(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // 正常取消，不记录错误
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台数据处理服务运行异常");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // 出错后等待
            }
        }

        _logger.LogInformation("后台数据处理服务已停止");
    }

    private async Task<bool> InitializeConfigurationAndSubscriptions(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var protocolConfigProvider = scope.ServiceProvider.GetRequiredService<IProtocolConfigProvider>();

            var config = await protocolConfigProvider.GetLatestConfigAsync(stoppingToken);
            if (config == null || string.IsNullOrWhiteSpace(config.ConfigJson))
            {
                _logger.LogWarning("未找到协议配置或配置为空");
                return false;
            }

            var protocolEntityList = JsonSerializer.Deserialize<List<ProtocolEntity>>(config.ConfigJson);
            if (protocolEntityList == null || !protocolEntityList.Any())
            {
                _logger.LogWarning("反序列化的协议配置为空");
                return false;
            }

            var topicHandles = new Dictionary<string, Func<ProtocolResult, CancellationToken, Task>>();
            foreach (var protocol in protocolEntityList)
            {
                if (!string.IsNullOrEmpty(protocol.ProtocolID))
                    topicHandles["workstation/" + protocol.ProtocolID] = (protocolResult, token) => ProcessDataAsync(protocolResult, token, protocol);
            }

            if (topicHandles.Any())
            {
                await _mqttSubscribeService.StartAsync(topicHandles, stoppingToken);
                _logger.LogInformation("成功初始化 {Count} 个MQTT主题订阅", topicHandles.Count);
                return true;
            }
            else
            {
                _logger.LogWarning("没有有效的协议ID用于订阅");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化配置和订阅时发生异常");
            return false;
        }
    }

    private DateTime _lastConfigTime = DateTime.MinValue;

    private async Task CheckForConfigurationUpdates(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var protocolConfigProvider = scope.ServiceProvider.GetRequiredService<IProtocolConfigProvider>();

            // 获取最新配置
            var latestConfig = await protocolConfigProvider.GetLatestConfigAsync(stoppingToken);
            if (latestConfig == null)
            {
                _logger.LogWarning("未获取到最新协议配置");
                return;
            }

            // 判断配置是否变化
            if (_lastConfigTime == DateTime.MinValue)
            {
                _lastConfigTime = latestConfig.SaveTime;
                return;
            }

            if (protocolConfigProvider.IsConfigChanged(latestConfig, _lastConfigTime))
            {
                _logger.LogInformation("检测到协议配置变更，正在重新初始化订阅...");
                _lastConfigTime = latestConfig.SaveTime;

                // 重新初始化订阅
                await InitializeConfigurationAndSubscriptions(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配置热更新时发生异常");
        }
    }


    private async Task ProcessDataAsync(ProtocolResult protocolResult, CancellationToken token, ProtocolEntity protocol)
    {
        if (protocolResult?.DeviceResults == null)
        {
            _logger.LogWarning("接收到空的协议结果数据");
            return;
        }

        try
        {
            foreach (var deviceResult in protocolResult.DeviceResults)
            {
                if (string.IsNullOrEmpty(deviceResult.EquipmentId) || deviceResult.PointResults == null) continue;

                var forwardDeviceResult = new Dictionary<string, object?>()
                {
                    { "DeviceId",deviceResult.EquipmentId },
                    { "Time", protocolResult.Time }
                };

                var device = protocol.Devices.FirstOrDefault(d => d.EquipmentId == deviceResult.EquipmentId);

                if (device == null) continue;

                foreach (var pointResult in deviceResult.PointResults)
                {
                    var point = device.Points.FirstOrDefault(p => p.Label == pointResult.Label);
                    if (point == null) continue;



                    if (!string.IsNullOrEmpty(pointResult.Label))
                        forwardDeviceResult[pointResult.Label] = pointResult.Value;
                }

                if (forwardDeviceResult.Any())
                {
                    var data = JsonSerializer.Serialize(forwardDeviceResult, _jsonOptions);
                    var topic = $"workstation/{deviceResult.EquipmentId}";

                    await _mqttPublishService.PublishAsync(topic, data, token);
                    _logger.LogDebug("已转发设备 {DeviceId} 的数据", deviceResult.EquipmentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理协议数据时发生异常");
        }
    }
}
//using KEDA_Common.Interfaces;
//using KEDA_Common.Model;
//using Microsoft.Extensions.Options;
//using System.Text.Json;

//namespace KEDA_Processing_Center.Services;

//public class DataProcessingWorker : BackgroundService
//{
//    private readonly ILogger<DataProcessingWorker> _logger;
//    private readonly IServiceProvider _serviceProvider;
//    private readonly IMqttPublishService _mqttPublishService;
//    private readonly IMqttSubscribeService _mqttSubscribeService;
//    private readonly JsonSerializerOptions options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
//    public DataProcessingWorker (ILogger<DataProcessingWorker> logger, IServiceProvider serviceProvider, IMqttPublishService mqttPublishService, IMqttSubscribeService mqttSubscribeService)
//    {
//        _logger = logger;
//        _serviceProvider = serviceProvider;
//        _mqttPublishService = mqttPublishService;
//        _mqttSubscribeService = mqttSubscribeService;
//    }

//    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("后台数据处理服务已启动");

//        using var scope = _serviceProvider.CreateScope();
//        var protocolConfigProvider = scope.ServiceProvider.GetRequiredService<IProtocolConfigProvider>();

//        var config = await protocolConfigProvider.GetLatestConfigAsync(stoppingToken);

//        var protocolEntityList = JsonSerializer.Deserialize<List<ProtocolEntity>>(config.ConfigJson);

//        var topicHandles = new Dictionary<string, Func<ProtocolResult, CancellationToken, Task>>();

//        foreach (var protocol in protocolEntityList)
//        {
//            topicHandles[protocol.ProtocolID] = ProcessingData;
//        }
//        await _mqttSubscribeService.StartAsync(topicHandles, stoppingToken);

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            _logger.LogInformation("正在处理数据...");
//            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//        }
//        _logger.LogInformation("后台数据处理服务已停止");
//    }

//    private async Task ProcessingData(ProtocolResult protocolResult, CancellationToken token)
//    {
//        foreach(var deviceResult in protocolResult.DeviceResults)
//        {
//            var deviceId = deviceResult.EquipmentId;
//            var forwardDeviceResult = new ForwardDeviceResult { DeviceId = deviceId, };

//            foreach (var pointResult in deviceResult.PointResults)
//            {
//                var forwardPointResult = new ForwardPointResult { Value = pointResult.Value, Label = pointResult.Label };
//                forwardDeviceResult.ForwardPoints.Add(forwardPointResult);
//            }

//            var data = JsonSerializer.Serialize(forwardDeviceResult, options);

//            await _mqttPublishService.PublishAsync("workstation/" + deviceId, data, token);
//        }
//    }
//}
