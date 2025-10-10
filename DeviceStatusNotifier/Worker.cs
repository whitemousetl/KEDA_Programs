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
        _heartbeatUrl = configuration["Heartbeat:Url"] ?? throw new ArgumentNullException("Heartbeat:Url ����ȱʧ");
    }

    private readonly Dictionary<string, string?> _lastDeviceUpdateTimes = new();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _workstationProvider.StartAsync(stoppingToken);

        while (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("�ȴ�����վ���ü���...");
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
                    _logger.LogWarning("�����豸״̬ʱ, ��mongo���ݿ��ѯ�Ĺ���վΪ��, 15�������...");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

                var wsDeviceIds = ws.Protocols
                    .SelectMany(p => p.Devices)
                    .Select(d => d.EquipmentID)
                    .ToHashSet();

                var res = await _deviceStatusRepository.GetAllLatestDeviceStatusAsync();

                bool hasChanged = false;
                foreach (var item in res)
                {
                    if (!wsDeviceIds.Contains(item.DeviceId))
                        continue;

                    if (!_lastDeviceUpdateTimes.TryGetValue(item.DeviceId, out var lastUpdateTime) ||
                        lastUpdateTime != item.UpdateTime)
                    {
                        hasChanged = true;
                        break;
                    }
                }

                if (hasChanged)
                {
                    foreach (var item in res)
                    {
                        if (!wsDeviceIds.Contains(item.DeviceId))
                            continue;

                        _lastDeviceUpdateTimes[item.DeviceId] = item.UpdateTime;
                    }

                    var edgeStatus = new NotificationModel
                    {
                        edge_id = ws.EdgeID,
                        edge_name = ws.EdgeName,
                        status = ((int)EdgeStatus.Online).ToString(),
                        msg = string.Empty,
                        time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        items = []
                    };

                    foreach (var item in res)
                    {
                        if (!wsDeviceIds.Contains(item.DeviceId))
                            continue;

                        var devName = FindDeviceNameById(ws, item.DeviceId);

                        var devStatus = new DeviceStatus
                        {
                            equipment_id = item.DeviceId,
                            equipment_name = devName,
                            equipment_status = item.Status == KEDA_Share.Enums.DevStatus.Online ? ((int)EquipmentStatus.Online).ToString() : ((int)EquipmentStatus.Offline).ToString(),
                            msg = item.Message,
                        };

                        edgeStatus.items.Add(devStatus);
                    }

                    try
                    {
                        // ���� JSON �ַ���
                        var json = JsonSerializer.Serialize(edgeStatus, new JsonSerializerOptions
                        {
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        // ���� POST ����
                        var response = await httpClient.PostAsync(_heartbeatUrl, content, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();

                            // ���Խ���Ӧ���ݷ����л������л���ȷ������������ʾ
                            try
                            {
                                using var doc = JsonDocument.Parse(responseContent);
                                var json1 = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                                {
                                    WriteIndented = false,
                                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                });
                                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                Console.WriteLine($"{time}�����ϱ��ɹ�����Ӧ����: {json1}");
                            }
                            catch
                            {
                                // ������Ǳ�׼ JSON��ֱ�����ԭʼ����
                                Console.WriteLine($"�����ϱ��ɹ�����Ӧ����: {responseContent}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("�����ϱ�ʧ��: {StatusCode}", response.StatusCode);
                        }

                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "�����ϱ��쳣: {Message}", ex.Message);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Worker��ѭ���쳣: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }


    static string FindDeviceNameById(Workstation ws, string deviceId)
    {
        return ws.Protocols
            .SelectMany(p => p.Devices)
            .FirstOrDefault(d => d.EquipmentID == deviceId)?.EquipmentName ?? string.Empty;
    }
}
