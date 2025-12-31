using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Processing_Center.Interfaces;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KEDA_Processing_Center.Services;

public class DeviceNotificationService : IDeviceNotificationService
{
    //private readonly string _heartbeatUrl;
    private readonly HttpClient _httpClient;
    private ILogger<DeviceNotificationService> _logger;
    private readonly IProtocolConfigProvider _protocolConfigProvider;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, };
    private readonly JsonSerializerOptions jsonSerializerOptions1 = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public DeviceNotificationService(ILogger<DeviceNotificationService> logger, IConfiguration configuration, IProtocolConfigProvider protocolConfigProvider)
    {
        _logger = logger;
        //_heartbeatUrl = configuration["Heartbeat:Url"] ?? throw new ArgumentNullException("Heartbeat:Url 配置缺失");
        _protocolConfigProvider = protocolConfigProvider;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Apifox/1.0.0 (https://apifox.com)");
        _httpClient.DefaultRequestHeaders.Connection.Clear();
        _httpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
    }

    public async Task MonitorDeviceStatusAsync(ConcurrentBag<ProtocolResult> results, CancellationToken token)
    {
        try
        {
            var workstationConfig = await _protocolConfigProvider.GetLatestWrokstationConfigAsync(token);

            if (workstationConfig == null || string.IsNullOrEmpty(workstationConfig.ConfigJson)) return;

            var ws = JsonSerializer.Deserialize<Workstation>(workstationConfig.ConfigJson);

            if (ws == null) return;

            // 设备状态上报（无论 hasChanged 是否为 true，都要上报）
            var edgeStatus = new NotificationModel
            {
                edge_id = ws.EdgeID,
                edge_name = ws.EdgeName,
                status = ((int)EdgeStatus.Online).ToString(),
                msg = string.Empty,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                items = [],
            };

            foreach (var item in results)
            {
                foreach (var dev in item.DeviceResults)
                {
                    var protocol = ws.Protocols.FirstOrDefault(p => p.Devices.Any(x => x.EquipmentID == dev.EquipmentId));
                    var device = protocol?.Devices.FirstOrDefault(x => x.EquipmentID == dev.EquipmentId);

                    if (device == null) continue;

                    string equipmentStatus = string.Empty;

                    if (dev.ReadIsSuccess && dev.SuccessPoints == dev.TotalPoints)
                        equipmentStatus = ((int)EquipmentStatus.Online).ToString();
                    else
                        equipmentStatus = ((int)EquipmentStatus.Offline).ToString();

                    var devStatus = new DeviceStatus
                    {
                        equipment_name = device.EquipmentName,
                        dev_type = device.Type,
                        equipment_id = device.EquipmentID,
                        equipment_status = equipmentStatus,
                        msg = dev.ErrorMsg,
                        time = item.EndTime ?? string.Empty,
                    };

                    edgeStatus.items.Add(devStatus);
                }
            }

            // 构造 JSON 字符串
            var json = JsonSerializer.Serialize(edgeStatus, jsonSerializerOptions);
            _logger.LogInformation($"本次读取设备状态: {json}");

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // 发送 POST 请求
            //var response = await _httpClient.PostAsync(_heartbeatUrl, content, token);

            //if (response.IsSuccessStatusCode)
            //{
            //    var responseContent = await response.Content.ReadAsStringAsync();
            //    try
            //    {
            //        using var doc = JsonDocument.Parse(responseContent);
            //        var json1 = JsonSerializer.Serialize(doc, jsonSerializerOptions1);
            //        _logger.LogInformation($"心跳上报成功，响应内容: {json1}");
            //    }
            //    catch
            //    {
            //        _logger.LogInformation($"心跳上报解析异常，响应内容: {responseContent}");
            //    }
            //}
            //else
            //{
            //    _logger.LogWarning("心跳上报失败: {StatusCode}", response.StatusCode);
            //}
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
        }
    }
}
