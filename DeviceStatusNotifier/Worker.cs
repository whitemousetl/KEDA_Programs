using KEDA_Share.Entity;
using KEDA_Share.Repository.Interfaces;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DeviceStatusNotifier;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDeviceStatusRepository _deviceStatusRepository;
    private readonly IWorkstationProvider _workstationProvider;
    private readonly string _heartbeatUrl;

    public Worker(ILogger<Worker> logger, IDeviceStatusRepository deviceStatusRepository, IWorkstationProvider workstationProvider, IConfiguration configuration)
    {
        _logger = logger;
        _deviceStatusRepository = deviceStatusRepository;
        _workstationProvider = workstationProvider;
        _heartbeatUrl = configuration["Heartbeat:Url"] ?? throw new ArgumentNullException("Heartbeat:Url 配置缺失");
    }

    private readonly Dictionary<string, string?> _lastDeviceUpdateTimes = [];
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly JsonSerializerOptions jsonSerializerOptions1 = new ()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _workstationProvider.StartAsync(stoppingToken);

        while (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("等待工作站配置加载...");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Apifox/1.0.0 (https://apifox.com)");
        httpClient.DefaultRequestHeaders.Connection.Clear();
        httpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ws = _workstationProvider.Current;

                if (ws == null)
                {
                    _logger.LogWarning("更新设备状态时, 从mongo数据库查询的工作站为空, 15秒后重试...");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

                var wsDeviceIds = ws.Protocols
                    .SelectMany(p => p.Devices)
                    .Select(d => d.EquipmentID)
                    .ToHashSet();

                var res = await _deviceStatusRepository.GetAllLatestDeviceStatusAsync();

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

                foreach (var deviceId in wsDeviceIds)
                {
                    var item = res.FirstOrDefault(d => d.DeviceId == deviceId);
                    var devMsg = FindDeviceNameById(ws, deviceId);

                    string equipmentStatus;
                    string message;
                    string time;

                    if (item == null)
                    {
                        // 没有采集到该设备，视为离线
                        equipmentStatus = ((int)EquipmentStatus.Offline).ToString();
                        message = "未采集到设备状态，视为离线";
                    }
                    else
                    {
                        // 判断时间是否变化
                        if (!_lastDeviceUpdateTimes.TryGetValue(deviceId, out var lastUpdateTime) || lastUpdateTime != item.UpdateTime)
                        {
                            // 时间有变化，根据仓库状态判断
                            equipmentStatus = item.Status == KEDA_Share.Enums.DevStatus.Online
                                ? ((int)EquipmentStatus.Online).ToString()
                                : ((int)EquipmentStatus.Offline).ToString();
                            message = item.Message;
                            _lastDeviceUpdateTimes[deviceId] = item.UpdateTime;
                        }
                        else
                        {
                            // 时间没变化，视为离线
                            equipmentStatus = ((int)EquipmentStatus.Offline).ToString();
                            message = "设备状态超时未更新，视为离线";
                        }
                    }

                    var devStatus = new DeviceStatus
                    {
                        equipment_name = devMsg.devNmae,
                        dev_type = devMsg.type,
                        equipment_id = deviceId,
                        equipment_status = equipmentStatus,
                        msg = message,
                        time = item?.UpdateTime ?? string.Empty,
                    };

                    edgeStatus.items.Add(devStatus);
                }


                // 构造 JSON 字符串
                var json = JsonSerializer.Serialize(edgeStatus, jsonSerializerOptions);
                _logger.LogInformation($"本次读取设备状态: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // 发送 POST 请求
                var response = await httpClient.PostAsync(_heartbeatUrl, content, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        var json1 = JsonSerializer.Serialize(doc, jsonSerializerOptions1);
                        _logger.LogInformation($"心跳上报成功，响应内容: {json1}");
                    }
                    catch
                    {
                        _logger.LogInformation($"心跳上报异常，响应内容: {responseContent}");
                    }
                }
                else
                {
                    _logger.LogWarning("心跳上报失败: {StatusCode}", response.StatusCode);
                }

                //bool hasChanged = false;
                //foreach (var item in res)
                //{
                //    if (!wsDeviceIds.Contains(item.DeviceId))
                //        continue;

                //    if (!_lastDeviceUpdateTimes.TryGetValue(item.DeviceId, out var lastUpdateTime) ||
                //        lastUpdateTime != item.UpdateTime)
                //    {
                //        hasChanged = true;
                //        break;
                //    }
                //}

                //if (hasChanged)
                //{
                //    foreach (var item in res)
                //    {
                //        if (!wsDeviceIds.Contains(item.DeviceId))
                //            continue;

                //        _lastDeviceUpdateTimes[item.DeviceId] = item.UpdateTime;
                //    }

                //    var edgeStatus = new NotificationModel
                //    {
                //        edge_id = ws.EdgeID,
                //        edge_name = ws.EdgeName,
                //        status = ((int)EdgeStatus.Online).ToString(),
                //        msg = string.Empty,
                //        time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //        items = [],
                //    };

                //    foreach (var item in res)
                //    {
                //        if (!wsDeviceIds.Contains(item.DeviceId))
                //            continue;

                //        var devMsg = FindDeviceNameById(ws, item.DeviceId);

                //        var devStatus = new DeviceStatus
                //        {
                //            equipment_name = devMsg.devNmae,
                //            dev_type = devMsg.type,
                //            equipment_id = item.DeviceId,
                //            equipment_status = item.Status == KEDA_Share.Enums.DevStatus.Online ? ((int)EquipmentStatus.Online).ToString() : ((int)EquipmentStatus.Offline).ToString(),
                //            msg = item.Message,
                //        };

                //        edgeStatus.items.Add(devStatus);
                //    }

                //    try
                //    {
                //        // 构造 JSON 字符串
                //        var json = JsonSerializer.Serialize(edgeStatus, jsonSerializerOptions);

                //        _logger.LogInformation($"本次读取设备状态: {json}");

                //        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                //        // 发送 POST 请求
                //        var response = await httpClient.PostAsync(_heartbeatUrl, content, stoppingToken);

                //        if (response.IsSuccessStatusCode)
                //        {
                //            var responseContent = await response.Content.ReadAsStringAsync();

                //            // 尝试将响应内容反序列化再序列化，确保中文正常显示
                //            try
                //            {
                //                using var doc = JsonDocument.Parse(responseContent);
                //                var json1 = JsonSerializer.Serialize(doc, jsonSerializerOptions1);
                //                _logger.LogInformation($"心跳上报成功，响应内容: {json1}");
                //            }
                //            catch
                //            {
                //                // 如果不是标准 JSON，直接输出原始内容
                //                _logger.LogInformation($"心跳上报异常，响应内容: {responseContent}");
                //            }
                //        }
                //        else
                //        {
                //            _logger.LogWarning("心跳上报失败: {StatusCode}", response.StatusCode);
                //        }

                //    }
                //    catch (Exception ex) when (ex is not OperationCanceledException)
                //    {
                //        _logger.LogError(ex, "心跳上报异常: {Message}", ex.Message);
                //    }
                //}
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Worker主循环异常: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }


    static (string devNmae, string type) FindDeviceNameById(Workstation ws, string deviceId)
    {
        var devName =  ws.Protocols
            .SelectMany(p => p.Devices)
            .FirstOrDefault(d => d.EquipmentID == deviceId)?.EquipmentName ?? string.Empty;

        var type = ws.Protocols
            .SelectMany(p => p.Devices)
            .FirstOrDefault(d => d.EquipmentID == deviceId)?.Type ?? string.Empty;

        return (devName, type); 
    }
}
