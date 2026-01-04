using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_Processing_CenterV2.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KEDA_Processing_CenterV2.Services;

public class DeviceNotificationService : IDeviceNotificationService
{
    private readonly MqttTopicSettings _topicOptions;
    private ILogger<DeviceNotificationService> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IWorkstationConfigProvider _workstationEntityProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, };

    public DeviceNotificationService(ILogger<DeviceNotificationService> logger, IWorkstationConfigProvider workstationEntityProvider, IMqttPublishService mqttPublishService, IOptions<MqttTopicSettings> topicOptions)
    {
        _logger = logger;
        _workstationEntityProvider = workstationEntityProvider;
        _mqttPublishService = mqttPublishService;
        _topicOptions = topicOptions.Value;
    }

    public async Task MonitorDeviceStatusAsync(ConcurrentBag<ProtocolResult> protocolResults, CancellationToken token)
    {
        try
        {
            var ws = await _workstationEntityProvider.GetLatestWrokstationAsync(token); // 获取最新工作站
            if (ws == null) return;
            var devNotificationModel = new NotificationModel
            {
                edge_id = ws.Id,
                edge_name = ws.Name,
                ip = ws.IpAddress,
                status = "1",
                msg = string.Empty,
                desc = string.Empty,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            foreach (var protocolResult in protocolResults)
            {
                ProcessDeviceResults(ws, devNotificationModel, protocolResult);
            }

            var data = JsonSerializer.Serialize(devNotificationModel, _jsonSerializerOptions);

            var edgeId = devNotificationModel.edge_id;
            var workstationStatusTopic = _topicOptions.WorkstationStatusPrefix + edgeId;

            await _mqttPublishService.PublishAsync(workstationStatusTopic, data, token);
            _logger.LogDebug("已定时转发设备 {DeviceId} 的数据到 {Topic}", edgeId, workstationStatusTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
        }
    }

    private static void ProcessDeviceResults(WorkstationDto? ws, NotificationModel devNotificationModel, ProtocolResult protocolResult)
    {
        foreach (var devResult in protocolResult.DeviceResults)
        {
            var protocol = ws?.Protocols.FirstOrDefault(p => p.Equipments.Any(x => x.Id == devResult.EquipmentId));
            var device = protocol?.Equipments.FirstOrDefault(x => x.Id == devResult.EquipmentId);

            if (device == null) continue;

            string equipmentStatus = string.Empty;

            if (devResult.ReadIsSuccess && devResult.SuccessPoints == devResult.TotalPoints)
                equipmentStatus = ((int)EquipmentStatus.Online).ToString();
            else
                equipmentStatus = ((int)EquipmentStatus.Offline).ToString();

            var devStatus = new DeviceStatus
            {
                equipment_name = device.Name,
                dev_type = ((int)device.EquipmentType).ToString(),
                equipment_id = device.Id,
                equipment_status = equipmentStatus,
                msg = devResult.ErrorMsg,
                time = devResult.EndTime ?? string.Empty,
            };

            devNotificationModel.items.Add(devStatus);
        }
    }
}
