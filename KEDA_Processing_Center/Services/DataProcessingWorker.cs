using DynamicExpresso;
using KEDA_Common.Helper;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Processing_Center.Interfaces;
using SqlSugar;
using System.Collections.Concurrent;
using System.Linq.Expressions;
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

    private readonly ConcurrentDictionary<string, DateTime> _lastMonitorTimes = new();
    private async Task<bool> InitializeConfigurationAndSubscriptions(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var protocolConfigProvider = scope.ServiceProvider.GetRequiredService<IProtocolConfigProvider>();
            var deviceNotification = scope.ServiceProvider.GetRequiredService<IDeviceNotificationService>();

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

            var topicHandles = new ConcurrentDictionary<string, Func<ProtocolResult, CancellationToken, Task>>();
            foreach (var protocol in protocolEntityList)
            {
                if (!string.IsNullOrEmpty(protocol.ProtocolID))
                {
                    topicHandles["edge/" + protocol.ProtocolID] = async (protocolResult, token) =>
                    {
                        // 主处理流程，必须等待
                        await ProcessDataAsync(protocolResult, protocol, token);

                        // ======= 设备状态监控频率限制（每个 protocol 限制 1 分钟一次） =======
                        var now = DateTime.UtcNow;

                        if (!_lastMonitorTimes.TryGetValue(protocol.ProtocolID, out var lastTime) ||
                            (now - lastTime).TotalSeconds >= 60)
                        {
                            // 更新执行时间，避免并发重复触发
                            _lastMonitorTimes[protocol.ProtocolID] = now;

                            _ = Task.Run(async () =>
                            {
                                await deviceNotification.MonitorDeviceStatusAsync([protocolResult], token);

                            }, token);
                        }
                    };
                }
            }

            if (topicHandles.Count != 0)
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

                // 持续重试订阅直到成功或取消
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (await InitializeConfigurationAndSubscriptions(stoppingToken))
                        break;
                    _logger.LogError("配置变更后初始化订阅失败，10秒后重试...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配置热更新时发生异常");
        }
    }

    private readonly ConcurrentDictionary<string, DateTime> _lastPublishTimes = new();
    private async Task ProcessDataAsync(ProtocolResult protocolResult, ProtocolEntity protocol, CancellationToken token)
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

                string timeStr = protocolResult.Time;
                var dt = DateTime.ParseExact(timeStr, "yyyy-MM-dd HH:mm:ss.fff", null);
                long timestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

                var forwardDeviceResult = new ConcurrentDictionary<string, object?>();
                forwardDeviceResult["DeviceId"] = deviceResult.EquipmentId;
                forwardDeviceResult["timestamp"] = timestamp;

                var device = protocol.Devices.FirstOrDefault(d => d.EquipmentId == deviceResult.EquipmentId);

                if (device == null) continue;

                // 收集虚拟点
                var virtualPoints = new ConcurrentBag<PointEntity>();

                foreach (var pointResult in deviceResult.PointResults)
                {
                    var point = device.Points.FirstOrDefault(p => p.Label == pointResult.Label);
                    if (point == null) continue;

                    if(pointResult.Value == null && point.Address != "VirtualPoint") continue;

                    //如果point的地址是VirtualPoint，则这是一个虚拟点，先跳过，等所有实际点处理完，然后再执行虚拟点的逻辑
                    // 判断是否为虚拟点
                    if (point.Address == "VirtualPoint")
                    {
                        // 跳过虚拟点，后续统一处理
                        virtualPoints.Add(point);
                        continue;
                    }

                    object? finalValue = pointResult?.Value;
                    try
                    {
                        if (!string.IsNullOrEmpty(point.Change) && point.Change.Equals("HEX2DEC", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("进入HEX转Decimal");
                            // 处理HEX转换
                            finalValue = ExpressionHelper.HexToDecimal(finalValue);
                            Console.WriteLine($"{point.Address} {point.Change} {finalValue}");
                        }
                        else if (point.Change.Equals("DEC2HEX", StringComparison.OrdinalIgnoreCase))
                        {
                            finalValue = ExpressionHelper.DecimalToHex(finalValue, false); // 或true带0x前缀
                            Console.WriteLine($"{point.Address} DEC2HEX {finalValue}");
                        }
                        else if (finalValue != null && ExpressionHelper.IsNumericType(finalValue))
                        {
                            double x;
                            if (finalValue is JsonElement je && je.ValueKind == JsonValueKind.Number) x = je.GetDouble();
                            else x = Convert.ToDouble(finalValue);
                            finalValue = ExpressionHelper.Eval(point.Change, x);//转换或四舍五入保留两位小数
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"表达式计算失败: {point.Change}, 原值: {pointResult?.Value}");
                        finalValue = pointResult?.Value;
                    }

                    if (!string.IsNullOrEmpty(pointResult?.Label))
                        forwardDeviceResult[pointResult.Label] = finalValue;
                }

                if (forwardDeviceResult.Count > 2)
                {
                    // 处理虚拟点
                    foreach (var point in virtualPoints)
                    {
                        if (string.IsNullOrWhiteSpace(point.Change)) continue;

                        // 1. 提取表达式中的变量名（不使用正则）
                        var expr = point.Change;
                        var variables = new ConcurrentBag<string>();//变量列表
                        int idx = 0;
                        while ((idx = expr.IndexOf('{', idx)) != -1)
                        {
                            int endIdx = expr.IndexOf('}', idx + 1);
                            if (endIdx == -1) break;
                            var varName = expr.Substring(idx + 1, endIdx - idx - 1);
                            if (!variables.Contains(varName))
                                variables.Add(varName);
                            idx = endIdx + 1;
                        }

                        // 2. 替换表达式中的{变量}为变量名
                        string dynamicExpr = expr;
                        foreach (var varName in variables)
                        {
                            dynamicExpr = dynamicExpr.Replace("{" + varName + "}", varName);
                        }

                        // 3. 构建解释器并传递变量
                        var interpreter = new Interpreter();
                        foreach (var varName in variables)
                        {
                            if (forwardDeviceResult.TryGetValue(varName, out var value))
                                interpreter.SetVariable(varName, value ?? 0);
                            else
                                interpreter.SetVariable(varName, 0); // 未找到变量，默认0
                        }

                        object? result = null;
                        try
                        {
                            result = interpreter.Eval(dynamicExpr);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"虚拟点表达式计算失败: {point.Change}");
                        }

                        if (!string.IsNullOrEmpty(point.Label))
                            forwardDeviceResult[point.Label] = result;
                    }

                    var data = JsonSerializer.Serialize(forwardDeviceResult, _jsonOptions);

                    // 实时发布到 control/{EquipmentId}
                    var controlTopic = $"control/{deviceResult.EquipmentId}";
                    await _mqttPublishService.PublishAsync(controlTopic, data, token);
                    _logger.LogDebug("已实时转发设备 {DeviceId} 的数据到 {Topic}", deviceResult.EquipmentId, controlTopic);

                    // 间隔一分钟发布到 workstation/{EquipmentId}
                    var workstationTopic = $"workstation/{deviceResult.EquipmentId}";
                    var now = DateTime.UtcNow;
                    if (!_lastPublishTimes.TryGetValue(deviceResult.EquipmentId, out var lastTime) || (now - lastTime).TotalSeconds >= 60)
                    {
                        await _mqttPublishService.PublishAsync(workstationTopic, data, token);
                        _lastPublishTimes.AddOrUpdate(deviceResult.EquipmentId, now, (_, old) => now);
                        _logger.LogDebug("已定时转发设备 {DeviceId} 的数据到 {Topic}", deviceResult.EquipmentId, workstationTopic);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理协议数据时发生异常");
        }
    }
}