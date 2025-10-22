//using CollectorService.CustomException;
//using CollectorService.Protocols;
//using KEDA_Share.Entity;
//using KEDA_Share.Enums;
//using KEDA_Share.Repository.Interfaces;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using DeviceStatus = KEDA_Share.Entity.DeviceStatus;

//namespace CollectorService;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IWorkstationProvider _workstationProvider;
//    private readonly IDeviceStatusRepository _deviceStatusRepository;
//    private readonly IDeviceResultRepository _deviceResultRepository;

//    private CancellationTokenSource? _collectCts;
//    private long _lastTimestamp = 0;

//    public Worker(ILogger<Worker> logger, IWorkstationProvider workstationProvider, IDeviceStatusRepository deviceStatusRepository, IDeviceResultRepository deviceResultRepository)
//    {
//        _logger = logger;
//        _workstationProvider = workstationProvider;
//        _deviceStatusRepository = deviceStatusRepository;
//        _deviceResultRepository = deviceResultRepository;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        //��������mongo���ݿ⣬���ҹ���վ���ã���ֵ��Current��ʧ�ܻ�����(û�����û��쳣)
//        _ = _workstationProvider.StartAsync(stoppingToken);

//        // �ȴ�����վ���ü������
//        if (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
//        {
//            _logger.LogInformation("�ȴ�����վ���ü���...");
//            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
//        }

//        //��ѭ����ѭ���壺�Ӳִ����õ�ǰ����վ,ִ�и���Э��Ĳɼ���������վ���õ�Timestamp����ʱ��ʹ��tokenȡ�����вɼ�������ִ�вɼ�
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var ws = _workstationProvider.Current;

//            if(ws == null)
//            {
//                _logger.LogWarning("�ɼ�ʱ, ����վΪ��, 15�������...");
//                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
//                continue;
//            }

//            if(ws.Timestamp != _lastTimestamp)
//            {
//                _logger.LogInformation("����վ�����Ѹ��£�����ȡ����ǰ������...");
//                _collectCts?.Cancel();
//                _collectCts = new();
//                _lastTimestamp = ws.Timestamp;
//                await Task.Delay(3000, stoppingToken);
//                _logger.LogInformation("���¿�ʼ�ɼ�...");

//                StartAllCollectors(ws, _collectCts?.Token);
//            }

//            await Task.Delay(1, stoppingToken);
//        }
//    }

//    private void StartAllCollectors(Workstation ws, CancellationToken? token)
//    {
//        foreach (var protocol in ws.Protocols)
//        {
//            // ����Э�����ͣ�δʵ�ֵ�Э������
//            if (!Enum.TryParse<ProtocolType>(protocol.ProtocolType, out var protocolType))
//            {
//                _logger.LogWarning($"����վ����Э�� {protocol.ProtocolType} δʵ��");
//                continue;
//            }

//            // ÿ��Э�鿪һ�������Ĳɼ�Task
//            _ = Task.Run(async () =>
//            {
//                // ͨ��Э�鹤������Э������
//                var driver = ProtocolDriverFactory.Create(protocolType);
//                if (driver == null)
//                {
//                    _logger.LogWarning($"Э������δʵ��: {protocol.ProtocolType}");
//                    return;
//                }

//                // ѭ���壺���������豸����ȡÿһ���㣬�����쳣�������Э���쳣��������ǰЭ��ĵ�ǰѭ�������ǲ�Ӱ�쵱ǰЭ�����һ��ѭ��
//                // Э����ѭ����ֱ����ȡ��
//                while (token == null || !token.Value.IsCancellationRequested)
//                {
//                    foreach (var dev in protocol.Devices)
//                    {
//                        // ��ʼ���豸״̬
//                        var deviceStatus = new DeviceStatus
//                        {
//                            DeviceId = dev.EquipmentID,
//                            Status = DevStatus.Online,
//                            Message = "",
//                            PointStatuses = dev.Points.Select(p => new PointStatus
//                            {
//                                Label = p.Label,
//                                Status = PointReadResult.Success,
//                                Message = ""
//                            }).ToList()
//                        };

//                        bool protocolFailed = false;
//                        bool protocolException = false;

//                        var devResult = new DeviceResult { DevId = dev.EquipmentID, PointResults = [] };

//                        var deviceStopwatch = Stopwatch.StartNew();

//                        // �ɼ�ÿ����
//                        foreach (var point in dev.Points)
//                        {
//                            var pointStatus = deviceStatus.PointStatuses.First(ps => ps.Label == point.Label);
//                            var pointStopwatch = Stopwatch.StartNew();

//                            var pointResult = new PointResult
//                            {
//                                Label = point.Label,
//                                DataType = Enum.Parse<DataType>(point.DataType),
//                            };
//                            try
//                            {
//                                // �ɼ�������
//                                var result = await driver.ReadAsync(protocol, dev, point, token ?? CancellationToken.None);

//                                pointResult.Result = result?.Value;

//                                devResult?.PointResults?.Add(pointResult);

//                                // �ɼ��ɹ������µ�״̬
//                                pointStatus.Status = PointReadResult.Success;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = "�ɼ��ɹ�";
//                            }
//                            catch (ProtocolFailedException ex)
//                            {
//                                // Э������ʧ�ܣ��豸״̬��Ϊ���ߣ����е�״̬��Ϊ�쳣
//                                _logger.LogError(ex, $"Э������ʧ��: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
//                                driver.Dispose();
//                                protocolFailed = true;
//                                deviceStatus.Status = DevStatus.Offline;
//                                deviceStatus.Message = ex.Message;
//                                // ���е㶼���Ϊ�쳣
//                                foreach (var ps in deviceStatus.PointStatuses)
//                                {
//                                    ps.Status = PointReadResult.Exception;
//                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                    ps.Message = ex.Message;
//                                }
//                                break;// ������ѭ��
//                            }
//                            catch (ProtocolException ex)
//                            {
//                                // Э���쳣���豸״̬��Ϊ�쳣�����е�״̬��Ϊ�쳣
//                                _logger.LogError(ex, $"Э���쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
//                                driver.Dispose();
//                                protocolException = true;
//                                deviceStatus.Status = DevStatus.Exception;
//                                deviceStatus.Message = ex.Message;
//                                foreach (var ps in deviceStatus.PointStatuses)
//                                {
//                                    ps.Status = PointReadResult.Exception;
//                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                    ps.Message = ex.Message;
//                                }
//                                break;// ������ѭ��
//                            }
//                            catch (PointFailedException ex)
//                            {
//                                // ���ȡʧ�ܣ�����ǰ��״̬��Ϊʧ�� 
//                                _logger.LogError(ex, $"���ȡʧ��: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
//                                pointStatus.Status = PointReadResult.Failed;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = ex.Message;
//                            }
//                            catch (PointException ex)
//                            {
//                                // ���쳣������ǰ��״̬��Ϊ�쳣
//                                _logger.LogError(ex, $"���쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
//                                pointStatus.Status = PointReadResult.Exception;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = ex.Message;
//                            }
//                            catch (Exception ex)
//                            {
//                                // δ֪�쳣������ǰ��״̬��Ϊ�쳣
//                                _logger.LogError(ex, $"δ֪�쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
//                                pointStatus.Status = PointReadResult.Exception;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = ex.Message;
//                            }
//                            finally
//                            {
//                                pointStopwatch.Stop();
//                                pointStatus.ElapsedMilliseconds = pointStopwatch.ElapsedMilliseconds;
//                            }
//                        }

//                        deviceStopwatch.Stop();
//                        deviceStatus.ElapsedMilliseconds = deviceStopwatch.ElapsedMilliseconds;

//                        // �������Э���쳣/ʧ�ܣ����������豸״̬���������豸���ֲɼ�
//                        if (protocolFailed || protocolException)
//                        {
//                            await _deviceStatusRepository.UpsertAsync(deviceStatus, token ?? CancellationToken.None);
//                            continue;
//                        }

//                        // �豸״̬�����ж�
//                        if (deviceStatus.PointStatuses.All(ps => ps.Status == PointReadResult.Success))
//                        {
//                            deviceStatus.Status = DevStatus.Online;
//                            deviceStatus.Message = "ȫ���ɼ���ɹ�";
//                        }
//                        else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Failed))
//                        {
//                            deviceStatus.Status = DevStatus.Exception;
//                            deviceStatus.Message = "���ڲɼ���ʧ��";
//                        }
//                        else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Exception))
//                        {
//                            deviceStatus.Status = DevStatus.Exception;
//                            deviceStatus.Message = "���ڲɼ����쳣";
//                        }
//                        // ���������豸״̬�����ݿ�
//                        await _deviceStatusRepository.UpsertAsync(deviceStatus, token ?? CancellationToken.None);

//                        await _deviceResultRepository.AddAsync(devResult, token ?? CancellationToken.None);
//                    }

//                    // ����Э��ɼ�����
//                    int cycleMs = 1000;
//                    if (int.TryParse(protocol.CollectCycle, out var ms) && ms > 0)
//                        cycleMs = ms;

//                    await Task.Delay(cycleMs, token ?? CancellationToken.None);
//                }

//            }, token ?? CancellationToken.None);
//        }
//    }
//}

using CollectorService.CustomException;
using CollectorService.Protocols;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using KEDA_Share.Repository.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using DeviceStatus = KEDA_Share.Entity.DeviceStatus;

namespace CollectorService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IWorkstationProvider _workstationProvider;
    private readonly IDeviceStatusRepository _deviceStatusRepository;
    private readonly IDeviceResultRepository _deviceResultRepository;

    private CancellationTokenSource? _collectCts;
    private long _lastTimestamp = 0;

    // ���ڹ������вɼ�����
    private readonly ConcurrentDictionary<string, Task> _collectorTasks = new();

    public Worker(ILogger<Worker> logger, IWorkstationProvider workstationProvider, IDeviceStatusRepository deviceStatusRepository, IDeviceResultRepository deviceResultRepository)
    {
        _logger = logger;
        _workstationProvider = workstationProvider;
        _deviceStatusRepository = deviceStatusRepository;
        _deviceResultRepository = deviceResultRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _workstationProvider.StartAsync(stoppingToken);

        if (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("�ȴ�����վ���ü���...");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var ws = _workstationProvider.Current;

            if (ws == null)
            {
                _logger.LogWarning("�ɼ�ʱ, ����վΪ��, 15�������...");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                continue;
            }

            if (ws.Timestamp != _lastTimestamp)
            {
                _logger.LogInformation("����վ�����Ѹ��£�����ȡ����ǰ������...");
                _collectCts?.Cancel();

                // �ȴ����вɼ�������ɲ�����
                if (_collectorTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(_collectorTasks.Values);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�ɼ�����ȡ��ʱ�����쳣");
                    }
                    _collectorTasks.Clear();
                }

                _collectCts = new();
                _lastTimestamp = ws.Timestamp;
                await Task.Delay(3000, stoppingToken);
                _logger.LogInformation("���¿�ʼ�ɼ�...");

                await StartAllCollectorsAsync(ws, _collectCts.Token);
            }

            await Task.Delay(10, stoppingToken); // �����ʵ��ӳ����������CPU��ת
        }
    }

    private async Task StartAllCollectorsAsync(Workstation ws, CancellationToken token)
    {
        foreach (var protocol in ws.Protocols)
        {
            // Ψһ��ʶ������Э������+IP+�˿ڻ�Э��ID
            var protocolKey = $"{protocol.ProtocolType}_{protocol.GetHashCode()}";

            if (!Enum.TryParse<ProtocolType>(protocol.ProtocolType, out var protocolType))
            {
                _logger.LogWarning($"����վ����Э�� {protocol.ProtocolType} δʵ��");
                continue;
            }

            if (protocol.Remark == "����")
                continue;

            var task = Task.Run(async () =>
            {
                var driver = ProtocolDriverFactory.Create(protocolType);
                try
                {
                    if (driver == null)
                    {
                        _logger.LogWarning($"Э������δʵ��: {protocol.ProtocolType}");
                        return;
                    }

                    while (!token.IsCancellationRequested)
                    {
                        foreach (var dev in protocol.Devices)
                        {
                            var deviceStatus = new DeviceStatus
                            {
                                DeviceId = dev.EquipmentID,
                                Status = DevStatus.Online,
                                Message = "",
                                PointStatuses = dev.Points.Select(p => new PointStatus
                                {
                                    Label = p.Label,
                                    Status = PointReadResult.Success,
                                    Message = ""
                                }).ToList()
                            };

                            bool protocolFailed = false;
                            bool protocolException = false;

                            var devResult = new DeviceResult { DevId = dev.EquipmentID, PointResults = [] };

                            var deviceStopwatch = Stopwatch.StartNew();

                            foreach (var point in dev.Points)
                            {
                                var pointStatus = deviceStatus.PointStatuses.First(ps => ps.Label == point.Label);
                                var pointStopwatch = Stopwatch.StartNew();

                                var pointResult = new PointResult
                                {
                                    Label = point.Label,
                                    DataType = Enum.Parse<DataType>(point.DataType),
                                };
                                try
                                {
                                    var result = await driver.ReadAsync(protocol, dev, point, token);

                                    pointResult.Result = result?.Value;
                                    devResult?.PointResults?.Add(pointResult);

                                    pointStatus.Status = PointReadResult.Success;
                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    pointStatus.Message = "�ɼ��ɹ�";
                                }
                                catch (ProtocolFailedException ex)
                                {
                                    _logger.LogError(ex, $"Э������ʧ��: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
                                    protocolFailed = true;
                                    deviceStatus.Status = DevStatus.Offline;
                                    deviceStatus.Message = ex.Message;
                                    foreach (var ps in deviceStatus.PointStatuses)
                                    {
                                        ps.Status = PointReadResult.Exception;
                                        pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        ps.Message = ex.Message;
                                    }
                                    break;
                                }
                                catch (ProtocolException ex)
                                {
                                    _logger.LogError(ex, $"Э���쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
                                    protocolException = true;
                                    deviceStatus.Status = DevStatus.Exception;
                                    deviceStatus.Message = ex.Message;
                                    foreach (var ps in deviceStatus.PointStatuses)
                                    {
                                        ps.Status = PointReadResult.Exception;
                                        pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        ps.Message = ex.Message;
                                    }
                                    break;
                                }
                                catch (PointFailedException ex)
                                {
                                    _logger.LogError(ex, $"���ȡʧ��: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
                                    pointStatus.Status = PointReadResult.Failed;
                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    pointStatus.Message = ex.Message;
                                }
                                catch (PointException ex)
                                {
                                    _logger.LogError(ex, $"���쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
                                    pointStatus.Status = PointReadResult.Exception;
                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    pointStatus.Message = ex.Message;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"δ֪�쳣: Э��={protocol.ProtocolType}, �豸={dev.EquipmentName}, ��={point.Label}");
                                    pointStatus.Status = PointReadResult.Exception;
                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    pointStatus.Message = ex.Message;
                                }
                                finally
                                {
                                    pointStopwatch.Stop();
                                    pointStatus.ElapsedMilliseconds = pointStopwatch.ElapsedMilliseconds;
                                }
                            }

                            deviceStopwatch.Stop();
                            deviceStatus.ElapsedMilliseconds = deviceStopwatch.ElapsedMilliseconds;

                            if (protocolFailed || protocolException)
                            {
                                await _deviceStatusRepository.UpsertAsync(deviceStatus, token);
                                continue;
                            }

                            if (deviceStatus.PointStatuses.All(ps => ps.Status == PointReadResult.Success))
                            {
                                deviceStatus.Status = DevStatus.Online;
                                deviceStatus.Message = "ȫ���ɼ���ɹ�";
                            }
                            else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Failed))
                            {
                                deviceStatus.Status = DevStatus.Exception;
                                deviceStatus.Message = "���ڲɼ���ʧ��";
                            }
                            else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Exception))
                            {
                                deviceStatus.Status = DevStatus.Exception;
                                deviceStatus.Message = "���ڲɼ����쳣";
                            }

                            await _deviceStatusRepository.UpsertAsync(deviceStatus, token);
                            await _deviceResultRepository.AddAsync(devResult, token);
                        }

                        int cycleMs = 1000;
                        if (int.TryParse(protocol.CollectCycle, out var ms) && ms > 0)
                            cycleMs = ms;

                        await Task.Delay(cycleMs, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Э��ɼ������쳣: {protocol.ProtocolType}");
                }
                finally
                {
                    driver?.Dispose();
                }
            }, token);

            _collectorTasks[protocolKey] = task;
        }

        await Task.CompletedTask;
    }
}
