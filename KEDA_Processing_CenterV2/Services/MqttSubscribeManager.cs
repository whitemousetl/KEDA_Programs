using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Converters;
using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Services;
using KEDA_Processing_CenterV2.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KEDA_Processing_CenterV2.Services;
public class MqttSubscribeManager : IMqttSubscribeManager
{
    private readonly object _topicLock = new();
    private readonly MqttTopicSettings _topicOptions;
    private readonly ILogger<MqttSubscribeManager> _logger;
    private readonly IMqttPublishManager _mqttPublishManager;
    private readonly IMqttSubscribeService _mqttSubscribeService;
    private readonly HashSet<string> _subscribedTopics = [];
    private readonly IWorkstationConfigProvider _workstationConfigProvider;
    private readonly IDeviceNotificationService _deviceNotificationService;
    private readonly ConcurrentDictionary<string, DateTime> _lastMonitorTimes = new();

    public MqttSubscribeManager(ILogger<MqttSubscribeManager> logger, IWorkstationConfigProvider workstationConfigProvider, IMqttSubscribeService mqttSubscribeService, IDeviceNotificationService deviceNotificationService, IMqttPublishManager mqttPublishManager)
    {
        _logger = logger;
        _workstationConfigProvider = workstationConfigProvider;
        _topicOptions = SharedConfigHelper.MqttTopicSettings;
        _mqttSubscribeService = mqttSubscribeService;
        _deviceNotificationService = deviceNotificationService;
        _mqttPublishManager = mqttPublishManager;
    }

    public async Task InitialAsync(CancellationToken stoppingToken)
    {
        // 1. 先订阅工作站配置下发主题，确保能收到配置
        await _mqttSubscribeService.AddSubscribeTopicAsync<string>(
            _topicOptions.WorkstationConfigSendPrefix, HandleWorkstationConfigAsync, stoppingToken);
        lock (_topicLock) { _subscribedTopics.Add(_topicOptions.WorkstationConfigSendPrefix); }

        // 2. 持续尝试初始化，直到成功或取消，订阅有配置的协议
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await InitializeConfigurationAndSubscriptions(stoppingToken))
                break;
            _logger.LogError("初始化配置和订阅失败，10秒后重试...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    public async Task<bool> InitializeConfigurationAndSubscriptions(CancellationToken stoppingToken) //初始化协议配置订阅，除了下发配置主题
    {
        try
        {
            var workstatioin = await _workstationConfigProvider.GetLatestWrokstationAsync(stoppingToken);
            if (workstatioin == null || !workstatioin.Protocols.Any())
            {
                _logger.LogWarning("反序列化的协议配置为空");
                return false;
            }

            // 1. 先取消所有已订阅主题，除了下发配置主题
            HashSet<string> topicsToRemove;
            lock (_topicLock)
            {
                topicsToRemove = _subscribedTopics.Where(t => t != _topicOptions.WorkstationConfigSendPrefix).ToHashSet();
                _subscribedTopics.RemoveWhere(t => t != _topicOptions.WorkstationConfigSendPrefix);
            }
            foreach (var topic in topicsToRemove) await _mqttSubscribeService.RemoveSubscribeTopicAsync(topic, stoppingToken);

            int protocolCount = 0;

            // 2. 重新订阅所有协议主题
            foreach (var protocol in workstatioin.Protocols)
            {
                if (!string.IsNullOrEmpty(protocol.Id))
                {
                    var topic = _topicOptions.EdgePrefix + protocol.Id;
                    await _mqttSubscribeService.AddSubscribeTopicAsync(topic, CreateProtocolHandler(protocol), stoppingToken);
                    lock (_topicLock) { _subscribedTopics.Add(topic); }
                    protocolCount++;
                }
            }

            if (_subscribedTopics.Count > 0)
            {
                _logger.LogInformation("成功初始化 {Count} 个MQTT主题订阅", _subscribedTopics.Count);
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

    private async Task HandleWorkstationConfigAsync(string payload, CancellationToken token) //处理下发的协议配置，存到questdb的WorkstationConfig中
    {
        WorkstationDto? ws = null;
        bool isSuccess = false;
        string status = "Success";
        string message = "配置已保存";
        string edgeId = string.Empty;

        try
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ProtocolJsonConverter());
            ws = JsonSerializer.Deserialize<WorkstationDto>(payload, options);

            if (ws == null) _logger.LogError("mom下发配置时，反序列化后工作站配置为空");
            else
            {
                // 检查Point.Label是否唯一
                var (isUnique, duplicateLabel) = CheckPointLabelUnique(ws);
                if (!isUnique)
                {
                    status = "Error";
                    message = $"Label '{duplicateLabel}' 重复！所有Device的Points的Label必须唯一。";
                    edgeId = ws.Id;
                    // 构造并发布响应
                    var repeatedResponse = new
                    {
                        EdgeID = edgeId,
                        IsSuccess = false,
                        Status = status,
                        Message = message,
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    var repeatedResponseJson = JsonSerializer.Serialize(repeatedResponse);
                    var repeatedResponseTopic = _topicOptions.WorkstationConfigResponsePrefix + edgeId;
                    await _mqttPublishManager.PublishConfigSavedResultAsync(repeatedResponseTopic, repeatedResponseJson, token);
                    return;
                }

                edgeId = ws.Id;
                var utcNow = DateTime.UtcNow;
                var shanghaiTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));
                var config = new WorkstationConfig
                {
                    ConfigJson = payload,
                    SaveTime = utcNow,
                    SaveTimeLocal = shanghaiTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
                await _workstationConfigProvider.SaveConfigAsync(config, token);
                isSuccess = true;
            }
        }
        catch (JsonException ex)
        {
            status = "Error";
            message = $"JSON反序列化失败: {ex.Message}";
            // 可尝试从payload中提取EdgeID
            edgeId = TryExtractEdgeId(payload);
        }
        catch (Exception ex)
        {
            status = "Error";
            message = $"处理异常: {ex.Message}";
            edgeId = ws?.Id ?? TryExtractEdgeId(payload);
        }

        // 构造并发布响应
        var response = new
        {
            EdgeID = edgeId,
            IsSuccess = isSuccess,
            Status = status,
            Message = message,
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        var responseJson = JsonSerializer.Serialize(response);

        var responseTopic = _topicOptions.WorkstationConfigResponsePrefix + edgeId;
        await _mqttPublishManager.PublishConfigSavedResultAsync(responseTopic, responseJson, token);
    }

    private static string TryExtractEdgeId(string json) //尝试获得工作站id
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EdgeID", out var edgeIdProp))
                return edgeIdProp.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    private static (bool IsUnique, string? DuplicateLabel) CheckPointLabelUnique(WorkstationDto ws) // 检查工作站协议配置的Label是否唯一
    {
        var labelSet = new HashSet<string>();
        foreach (var device in ws.Protocols.SelectMany(p => p.Equipments))
        {
            foreach (var point in device.Parameters)
            {
                if (!labelSet.Add(point.Label))
                {
                    return (false, point.Label);  // 返回重复的 Label
                }
            }
        }
        return (true, null);
    }

    private Func<ProtocolResult, CancellationToken, Task> CreateProtocolHandler(ProtocolDto protocol) //创建协议处理器，1处理数据，2监控状态
    {
        return async (protocolResult, token) =>
        {
            // 主处理流程，必须等待
            await _mqttPublishManager.ProcessDataAsync(protocolResult, protocol, token); //把协议结果转换，清洗,发布

            // ======= 设备状态监控频率限制（每个 protocol 限制 1 分钟一次） =======
            var now = DateTime.UtcNow;

            if (!_lastMonitorTimes.TryGetValue(protocol.Id, out var lastTime) ||
                (now - lastTime).TotalSeconds >= 60)
            {
                // 更新执行时间，避免并发重复触发
                _lastMonitorTimes[protocol.Id] = now;

                _ = Task.Run(async () =>
                {
                    await _deviceNotificationService.MonitorDeviceStatusAsync([protocolResult], token); // 监控设备状态，发布MQTT设备主题

                }, token);
            }
        };
    }
}