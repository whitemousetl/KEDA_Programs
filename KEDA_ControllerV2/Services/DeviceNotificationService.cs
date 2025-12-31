using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class DeviceNotificationService : IDeviceNotificationService
{
    private readonly MqttTopicSettings _topicOptions;
    private ILogger<DeviceNotificationService> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IWorkstationConfigProvider _workstationEntityProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, };

    public DeviceNotificationService(ILogger<DeviceNotificationService> logger, IWorkstationConfigProvider workstationEntityProvider, IMqttPublishService mqttPublishService)
    {
        _logger = logger;
        _workstationEntityProvider = workstationEntityProvider;
        _mqttPublishService = mqttPublishService;
        _topicOptions = SharedConfigHelper.MqttTopicSettings;
    }

    public async Task MonitorDeviceStatusAsync(ProtocolResult protocolStatus, CancellationToken token)
    {
        try
        {
            var ws = await _workstationEntityProvider.GetLatestWrokstationAsync(token); // 获取最新工作站
            if (ws == null) return;

            // 构造新的列表，PointResults 设为空列表
            // 构造新的 List<DeviceResult>，PointResults 设为空列表
            var deviceResults = protocolStatus.DeviceResults
                .Select(dev => new DeviceResult
                {
                    EquipmentId = dev.EquipmentId,
                    EquipmentName = dev.EquipmentName,
                    PointResults = new List<PointResult>(), // 设为空列表
                    ReadIsSuccess = dev.ReadIsSuccess,
                    ErrorMsg = dev.ErrorMsg,
                    ElapsedMs = dev.ElapsedMs,
                    TotalPoints = dev.TotalPoints,
                    SuccessPoints = dev.SuccessPoints,
                    FailedPoints = dev.FailedPoints,
                    StartTime = dev.StartTime,
                    EndTime = dev.EndTime,
                    Metadata = dev.Metadata != null
                        ? new Dictionary<string, object>(dev.Metadata)
                        : new Dictionary<string, object>()
                })
                .ToList();

            var data = JsonSerializer.Serialize(deviceResults, _jsonSerializerOptions);

            var edgeId = ws.EdgeId;
            var workstationStatusTopic = _topicOptions.WorkstationStatusPrefix + edgeId;

            await _mqttPublishService.PublishAsync(workstationStatusTopic, data, token);
            _logger.LogDebug("已定时转发设备 {DeviceId} 的数据到 {Topic}", edgeId, workstationStatusTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
        }
    }

    //public async Task MonitorDeviceStatusAsync(ConcurrentBag<ProtocolResult> protocolResults, CancellationToken token)
    //{
    //    try
    //    {
    //        var ws = await _workstationEntityProvider.GetLatestWrokstationAsync(token); // 获取最新工作站
    //        if (ws == null) return;
    //        var devNotificationModel = new NotificationModel
    //        {
    //            edge_id = ws.EdgeId,
    //            edge_name = ws.EdgeName,
    //            ip = ws.Ip,
    //            status = "1",
    //            msg = string.Empty,
    //            desc = string.Empty,
    //            time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    //        };

    //        foreach (var protocolResult in protocolResults)
    //        {
    //            ProcessDeviceResults(ws, devNotificationModel, protocolResult);
    //        }

    //        var data = JsonSerializer.Serialize(devNotificationModel, _jsonSerializerOptions);

    //        var edgeId = devNotificationModel.edge_id;
    //        var workstationStatusTopic = _topicOptions.WorkstationStatusPrefix + edgeId;

    //        await _mqttPublishService.PublishAsync(workstationStatusTopic, data, token);
    //        _logger.LogDebug("已定时转发设备 {DeviceId} 的数据到 {Topic}", edgeId, workstationStatusTopic);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
    //    }
    //}

    private static void ProcessDeviceResults(Workstation? ws, NotificationModel devNotificationModel, ProtocolResult protocolResult)
    {
        foreach (var devResult in protocolResult.DeviceResults)
        {
            var protocol = ws?.Protocols.FirstOrDefault(p => p.Devices.Any(x => x.EquipmentId == devResult.EquipmentId));
            var device = protocol?.Devices.FirstOrDefault(x => x.EquipmentId == devResult.EquipmentId);

            if (device == null) continue;

            string equipmentStatus = string.Empty;

            if (devResult.ReadIsSuccess && devResult.SuccessPoints == devResult.TotalPoints)
                equipmentStatus = ((int)EquipmentStatus.Online).ToString();
            else
                equipmentStatus = ((int)EquipmentStatus.Offline).ToString();

            var devStatus = new DeviceStatus
            {
                equipment_name = device.EquipmentName,
                dev_type = ((int)device.EquipmentType).ToString(),
                equipment_id = device.EquipmentId,
                equipment_status = equipmentStatus,
                msg = devResult.ErrorMsg,
                time = devResult.EndTime ?? string.Empty,
            };

            devNotificationModel.items.Add(devStatus);
        }
    }
}