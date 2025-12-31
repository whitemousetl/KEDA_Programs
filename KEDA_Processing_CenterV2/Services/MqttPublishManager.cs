using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_Processing_CenterV2.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace KEDA_Processing_CenterV2.Services;
public class MqttPublishManager : IMqttPublishManager
{
    private readonly MqttTopicSettings _topicOptions;
    private readonly ILogger<MqttPublishManager> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IDeviceDataProcessor _deviceDataProcessor;
    private readonly IDeviceDataStorageService _deviceDataStorageService;
    private readonly ConcurrentDictionary<string, DateTime> _lastPublishTimes = new();

    public MqttPublishManager(ILogger<MqttPublishManager> logger, IOptions<MqttTopicSettings> topicOptions, IMqttPublishService mqttPublishService, IDeviceDataProcessor deviceDataProcessor, IDeviceDataStorageService deviceDataStorageService)
    {
        _logger = logger;
        _topicOptions = topicOptions.Value;
        _mqttPublishService = mqttPublishService;
        _deviceDataProcessor = deviceDataProcessor;
        _deviceDataStorageService = deviceDataStorageService;
    }

    public async Task ProcessDataAsync(ProtocolResult protocolResult, Protocol protocol, CancellationToken token)
    {
        if (protocolResult?.DeviceResults == null)
        {
            _logger.LogWarning("接收到空的协议结果数据");
            return;
        }

        try
        {
            // 只负责调度和发布， 清洗转换，返回要发布的结果
            var publishDataList = _deviceDataProcessor.Process(protocolResult, protocol, token);
            await PublishDeviceDataAsync(publishDataList, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布处理协议数据的主题时发生异常");
        }
    }

    public async Task PublishConfigSavedResultAsync(string topic, string result, CancellationToken token) => await _mqttPublishService.PublishAsync(topic, result, token);

    private async Task PublishDeviceDataAsync(ConcurrentDictionary<string, string> dataDevIds, CancellationToken token)
    {
        foreach (var dataDevId in dataDevIds)
        {
            var data = dataDevId.Value;
            var devId = dataDevId.Key;
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(devId)) return;

            // 实时发布到 control/{EquipmentId}
            var controlTopic = _topicOptions.ControlPrefix + devId;
            await _mqttPublishService.PublishAsync(controlTopic, data, token);
            _logger.LogDebug("已实时转发设备 {DeviceId} 的数据到 {Topic}", devId, controlTopic);

            // ========== 存储到数据库 ==========
            await _deviceDataStorageService.SaveDeviceDataAsync(devId, data, token);

            // 间隔一分钟发布到 workstation/data/{EquipmentId}
            var workstationTopic = _topicOptions.WorkstationDataPrefix + devId;
            var now = DateTime.UtcNow;
            if (!_lastPublishTimes.TryGetValue(devId, out var lastTime) || (now - lastTime).TotalSeconds >= 60)
            {
                await _mqttPublishService.PublishAsync(workstationTopic, data, token);
                _lastPublishTimes.AddOrUpdate(devId, now, (_, old) => now);
                _logger.LogDebug("已定时转发设备 {DeviceId} 的数据到 {Topic}", devId, workstationTopic);
            }
        }
    }
}
