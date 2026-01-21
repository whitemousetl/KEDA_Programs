using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Services;

public class MqttPublishManager : IMqttPublishManager
{
    private readonly MqttTopicSettings _topicOptions;
    private readonly ILogger<MqttPublishManager> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IEquipmentDataProcessor _equipmentDataProcessor;
    private readonly ConcurrentDictionary<string, DateTime> _lastPublishTimes = new();

    public MqttPublishManager(ILogger<MqttPublishManager> logger, IMqttPublishService mqttPublishService, IEquipmentDataProcessor equipmentDataProcessor)
    {
        _logger = logger;
        _topicOptions = SharedConfigHelper.MqttTopicSettings;
        _mqttPublishService = mqttPublishService;
        _equipmentDataProcessor = equipmentDataProcessor;
    }

    public async Task ProcessDataAsync(ProtocolResult protocolResult, List<EquipmentDto> equipments, CancellationToken token)
    {
        if (protocolResult?.EquipmentResults == null)
        {
            _logger.LogWarning("接收到空的协议结果数据");
            return;
        }

        try
        {
            // 只负责调度和发布， 清洗转换，返回要发布的结果
            var publishDataList = _equipmentDataProcessor.Process(protocolResult, equipments, token);
            await PublishEquipmentDataAsync(publishDataList, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布处理协议数据的主题时发生异常");
        }
    }

    public async Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token) => await _mqttPublishService.PublishAsync(topic, result, token);

    private async Task PublishEquipmentDataAsync(ConcurrentDictionary<string, string> dataEquipmentIds, CancellationToken token)
    {
        foreach (var dataEquipmentId in dataEquipmentIds)
        {
            var data = dataEquipmentId.Value;
            var equipmentId = dataEquipmentId.Key;
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(equipmentId)) continue;


            // 反序列化data为字典
            Dictionary<string, object>? dataDict = null;
            try
            {
                dataDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"设备 {equipmentId} 的数据无法反序列化为字典，跳过。");
                continue;
            }

            if (dataDict == null)
            {
                _logger.LogWarning($"设备 {equipmentId} 的数据为空字典，跳过。");
                continue;
            }

            // 排除 "timestamp" 和 "EquipmentId" 后的有效数据key
            var validKeys = dataDict.Keys
                .Where(k => !string.Equals(k, "timestamp", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(k, "EquipmentId", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (validKeys.Count <= 1)
            {
                _logger.LogWarning($"设备 {equipmentId} 的有效数据点数量不足，跳过发布和存储操作。");
                continue;
            }

            // 检查这些key中至少有一个value不为空
            bool hasValidValue = validKeys.Any(k =>
            {
                var v = dataDict[k];
                return v is not null && !string.IsNullOrWhiteSpace(v.ToString());
            });

            if (!hasValidValue)
            {
                _logger.LogWarning($"设备 {equipmentId} 的有效数据点值全部为空，跳过发布和存储操作。");
                continue;
            }

            // 实时发布到 control/{EquipmentId}
            var controlTopic = _topicOptions.ControlPrefix + equipmentId;
            await _mqttPublishService.PublishAsync(controlTopic, data, token);
            _logger.LogDebug($"已实时转发设备 {equipmentId} 的数据到 {controlTopic}");

            // ========== 存储到数据库 ==========
            //await _equipmentDataStorageService.SaveEquipmentDataAsync(equipmentId, data, token);

            // 间隔一分钟发布到 workstation/data/{EquipmentId}
            var workstationTopic = _topicOptions.WorkstationDataPrefix + equipmentId;
            var now = DateTime.UtcNow;
            if (!_lastPublishTimes.TryGetValue(equipmentId, out var lastTime) || (now - lastTime).TotalSeconds >= 60)
            {
                await _mqttPublishService.PublishAsync(workstationTopic, data, token);
                _lastPublishTimes.AddOrUpdate(equipmentId, now, (_, old) => now);
                _logger.LogDebug($"已定时转发设备 {equipmentId} 的数据到 {workstationTopic}");
            }
        }
    }
}
