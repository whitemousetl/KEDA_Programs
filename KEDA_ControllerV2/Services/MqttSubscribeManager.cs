using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Converters;
using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.MqttResponses;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Utilities;
using KEDA_ControllerV2.Interfaces;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class MqttSubscribeManager : IMqttSubscribeManager
{
    private readonly MqttTopicSettings _topicOptions;
    private readonly IProtocolTaskManager _protocolTaskManager;
    private readonly ILogger<MqttSubscribeManager> _logger;
    private readonly IMqttPublishManager _mqttPublishManager;
    private readonly IMqttSubscribeService _mqttSubscribeService;
    private readonly IWriteTaskManager _writeTaskManager;
    private readonly IWorkstationConfigProvider _workstationConfigProvider;

    public MqttSubscribeManager(ILogger<MqttSubscribeManager> logger, IWorkstationConfigProvider workstationConfigProvider, IMqttSubscribeService mqttSubscribeService, IMqttPublishManager mqttPublishManager, IWriteTaskManager writeTaskManager, IProtocolTaskManager protocolTaskManager)
    {
        _logger = logger;
        _workstationConfigProvider = workstationConfigProvider;
        _topicOptions = SharedConfigHelper.MqttTopicSettings;
        _mqttSubscribeService = mqttSubscribeService;
        _mqttPublishManager = mqttPublishManager;
        _writeTaskManager = writeTaskManager;
        _protocolTaskManager = protocolTaskManager;
    }

    public async Task<bool> SubscribeConfigAndWriteTopicsAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 直接订阅下发配置主题
            await _mqttSubscribeService.AddSubscribeTopicAsync<string>(
                _topicOptions.WorkstationConfigSendPrefix, HandleWorkstationConfigAsync, stoppingToken);

            // 直接订阅写主题
            await _mqttSubscribeService.AddSubscribeTopicAsync<string>(
                _topicOptions.ProtocolWritePrefix, TriggerWriteTaskAsync, stoppingToken);

            _logger.LogInformation("成功订阅下发配置主题和写主题");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅主题时发生异常");
            return false;
        }
    }

    private async Task HandleWorkstationConfigAsync(string payload, CancellationToken token) //处理下发的协议配置，存到questdb的WorkstationConfig中
    {
        WorkstationDto? ws = null;
        bool isSuccess = false;
        string message = "配置已保存";
        string workstationId = string.Empty;

        try
        {
            ws = JsonSerializer.Deserialize<WorkstationDto>(payload, JsonOptionsProvider.WorkstationOptions);

            if (ws == null) _logger.LogError("mom下发配置时，反序列化后工作站配置为空");
            else
            {
                // 检查Point.Label是否唯一
                var (isUnique, duplicateLabel) = CheckPointLabelUnique(ws);
                if (!isUnique)
                {
                    message = $"Label '{duplicateLabel}' 重复！所有Device的Points的Label必须唯一。";
                    workstationId = ws.Id;
                    // 构造并发布响应
                    var repeatedResponse = new ConfigSaveResponse
                    {
                        WorkstationId = workstationId,
                        IsSuccess = false,
                        Message = message,
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    var repeatedResponseJson = JsonSerializer.Serialize(repeatedResponse);
                    var repeatedResponseTopic = _topicOptions.WorkstationConfigResponsePrefix + workstationId;
                    await _mqttPublishManager.PublishConfigSavedResultAsync(repeatedResponseTopic, repeatedResponseJson, token);
                    return;
                }

                workstationId = ws.Id;
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
            message = $"JSON反序列化失败: {ex.Message}";
            // 可尝试从payload中提取EdgeId
            workstationId = TryExtractEdgeId(payload);
        }
        catch (Exception ex)
        {
            message = $"处理异常: {ex.Message}";
            workstationId = ws?.Id ?? TryExtractEdgeId(payload);
        }

        // 构造并发布响应
        var response = new ConfigSaveResponse
        {
            WorkstationId = workstationId,
            IsSuccess = isSuccess,
            Message = message,
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        var responseJson = JsonSerializer.Serialize(response);

        var responseTopic = _topicOptions.WorkstationConfigResponsePrefix + workstationId;
        await _mqttPublishManager.PublishConfigSavedResultAsync(responseTopic, responseJson, token);

        // 持续重试直到成功或取消
        while (!token.IsCancellationRequested)
        {
            if (await _protocolTaskManager.RestartAllProtocolsAsync(token))
                break;
            _logger.LogError("协议采集任务初始化失败，10秒后重试...");
            await Task.Delay(TimeSpan.FromSeconds(10), token);
        }
    }

    private static string TryExtractEdgeId(string json) //尝试获得工作站id
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EdgeId", out var edgeIdProp))
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

    public async Task TriggerWriteTaskAsync(string payload, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("收到的写任务 payload 为空，已跳过。");
            return;
        }

        WriteTask? writeTaskEntity;
        try
        {
            writeTaskEntity = JsonSerializer.Deserialize<WriteTask>(payload, JsonOptionsProvider.WorkstationOptions);
            if (writeTaskEntity == null)
            {
                _logger.LogWarning("写任务 payload 反序列化后为 null，已跳过。payload: {Payload}", payload);
                return;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "写任务 payload 反序列化失败，已跳过。payload: {Payload}", payload);
            return;
        }

        await _writeTaskManager.TriggerWriteTaskAsync(writeTaskEntity, token);
    }
}