using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class EquipmentNotificationService : IEquipmentNotificationService
{
    private readonly MqttTopicSettings _topicOptions;
    private ILogger<EquipmentNotificationService> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly IWorkstationConfigProvider _workstationEntityProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, };

    public EquipmentNotificationService(ILogger<EquipmentNotificationService> logger, IWorkstationConfigProvider workstationEntityProvider, IMqttPublishService mqttPublishService)
    {
        _logger = logger;
        _workstationEntityProvider = workstationEntityProvider;
        _mqttPublishService = mqttPublishService;
        _topicOptions = SharedConfigHelper.MqttTopicSettings;
    }

    public async Task MonitorEquipmentStatusAsync(ProtocolResult protocolStatus, CancellationToken token)
    {
        try
        {
            var ws = await _workstationEntityProvider.GetLatestWrokstationAsync(token); // 获取最新工作站
            if (ws == null) return;

            // 构造新的列表，PointResults 设为空列表
            // 构造新的 List<EquipmentResult>，PointResults 设为空列表
            var equipmentResults = protocolStatus.EquipmentResults
                .Select(equipment => new EquipmentResult
                {
                    EquipmentId = equipment.EquipmentId,
                    EquipmentName = equipment.EquipmentName,
                    PointResults = new List<PointResult>(), // 设为空列表
                    ReadIsSuccess = equipment.ReadIsSuccess,
                    ErrorMsg = equipment.ErrorMsg,
                    ElapsedMs = equipment.ElapsedMs,
                    TotalPoints = equipment.TotalPoints,
                    SuccessPoints = equipment.SuccessPoints,
                    FailedPoints = equipment.FailedPoints,
                    StartTime = equipment.StartTime,
                    EndTime = equipment.EndTime,
                    Metadata = equipment.Metadata != null
                        ? new Dictionary<string, object>(equipment.Metadata)
                        : new Dictionary<string, object>()
                })
                .ToList();

            var data = JsonSerializer.Serialize(equipmentResults, _jsonSerializerOptions);

            var workstationId = ws.Id;
            var workstationStatusTopic = _topicOptions.WorkstationStatusPrefix + workstationId;

            await _mqttPublishService.PublishAsync(workstationStatusTopic, data, token);
            _logger.LogDebug($"已定时转发设备 {workstationId} 的数据到 {workstationStatusTopic}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
        }
    }
}