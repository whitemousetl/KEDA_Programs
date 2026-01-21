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
//        //尝试连接mongo数据库，查找工作站配置，赋值给Current，失败会重连(没有配置或异常)
//        _ = _workstationProvider.StartAsync(stoppingToken);

//        // 等待工作站配置加载完成
//        if (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
//        {
//            _logger.LogInformation("等待工作站配置加载...");
//            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
//        }

//        //主循环，循环体：从仓储层获得当前工作站,执行各个协议的采集，当工作站配置的Timestamp更改时，使用token取消所有采集，重新执行采集
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var ws = _workstationProvider.Current;

//            if(ws == null)
//            {
//                _logger.LogWarning("采集时, 工作站为空, 15秒后重试...");
//                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
//                continue;
//            }

//            if(ws.Timestamp != _lastTimestamp)
//            {
//                _logger.LogInformation("工作站配置已更新，正在取消当前的任务...");
//                _collectCts?.Cancel();
//                _collectCts = new();
//                _lastTimestamp = ws.Timestamp;
//                await Task.Delay(3000, stoppingToken);
//                _logger.LogInformation("重新开始采集...");

//                StartAllCollectors(ws, _collectCts?.Token);
//            }

//            await Task.Delay(1, stoppingToken);
//        }
//    }

//    private void StartAllCollectors(Workstation ws, CancellationToken? token)
//    {
//        foreach (var protocol in ws.Protocols)
//        {
//            // 解析协议类型，未实现的协议跳过
//            if (!Enum.TryParse<ProtocolType>(protocol.ProtocolType, out var protocolType))
//            {
//                _logger.LogWarning($"工作站存在协议 {protocol.ProtocolType} 未实现");
//                continue;
//            }

//            // 每个协议开一个独立的采集Task
//            _ = Task.Run(async () =>
//            {
//                // 通过协议工厂创建协议驱动
//                var driver = ProtocolDriverFactory.Create(protocolType);
//                if (driver == null)
//                {
//                    _logger.LogWarning($"协议驱动未实现: {protocol.ProtocolType}");
//                    return;
//                }

//                // 循环体：遍历所有设备，读取每一个点，捕获异常，如果是协议异常，跳过当前协议的当前循环，但是不影响当前协议的下一次循环
//                // 协议主循环，直到被取消
//                while (token == null || !token.Value.IsCancellationRequested)
//                {
//                    foreach (var dev in protocol.Devices)
//                    {
//                        // 初始化设备状态
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

//                        // 采集每个点
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
//                                // 采集点数据
//                                var result = await driver.ReadAsync(protocol, dev, point, token ?? CancellationToken.None);

//                                pointResult.Result = result?.Value;

//                                devResult?.PointResults?.Add(pointResult);

//                                // 采集成功，更新点状态
//                                pointStatus.Status = PointReadResult.Success;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = "采集成功";
//                            }
//                            catch (ProtocolFailedException ex)
//                            {
//                                // 协议连接失败，设备状态设为离线，所有点状态设为异常
//                                _logger.LogError(ex, $"协议连接失败: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
//                                driver.Dispose();
//                                protocolFailed = true;
//                                deviceStatus.Status = DevStatus.Offline;
//                                deviceStatus.Message = ex.Message;
//                                // 所有点都标记为异常
//                                foreach (var ps in deviceStatus.PointStatuses)
//                                {
//                                    ps.Status = PointReadResult.Exception;
//                                    pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                    ps.Message = ex.Message;
//                                }
//                                break;// 跳出点循环
//                            }
//                            catch (ProtocolException ex)
//                            {
//                                // 协议异常，设备状态设为异常，所有点状态设为异常
//                                _logger.LogError(ex, $"协议异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
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
//                                break;// 跳出点循环
//                            }
//                            catch (PointFailedException ex)
//                            {
//                                // 点读取失败，仅当前点状态设为失败 
//                                _logger.LogError(ex, $"点读取失败: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
//                                pointStatus.Status = PointReadResult.Failed;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = ex.Message;
//                            }
//                            catch (PointException ex)
//                            {
//                                // 点异常，仅当前点状态设为异常
//                                _logger.LogError(ex, $"点异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
//                                pointStatus.Status = PointReadResult.Exception;
//                                pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                                pointStatus.Message = ex.Message;
//                            }
//                            catch (Exception ex)
//                            {
//                                // 未知异常，仅当前点状态设为异常
//                                _logger.LogError(ex, $"未知异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
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

//                        // 如果发生协议异常/失败，立即保存设备状态并跳过本设备本轮采集
//                        if (protocolFailed || protocolException)
//                        {
//                            await _deviceStatusRepository.UpsertAsync(deviceStatus, token ?? CancellationToken.None);
//                            continue;
//                        }

//                        // 设备状态最终判定
//                        if (deviceStatus.PointStatuses.All(ps => ps.Status == PointReadResult.Success))
//                        {
//                            deviceStatus.Status = DevStatus.Online;
//                            deviceStatus.Message = "全部采集点成功";
//                        }
//                        else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Failed))
//                        {
//                            deviceStatus.Status = DevStatus.Exception;
//                            deviceStatus.Message = "存在采集点失败";
//                        }
//                        else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Exception))
//                        {
//                            deviceStatus.Status = DevStatus.Exception;
//                            deviceStatus.Message = "存在采集点异常";
//                        }
//                        // 保存或更新设备状态到数据库
//                        await _deviceStatusRepository.UpsertAsync(deviceStatus, token ?? CancellationToken.None);

//                        await _deviceResultRepository.AddAsync(devResult, token ?? CancellationToken.None);
//                    }

//                    // 控制协议采集周期
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
using CollectorService.Helper;
using CollectorService.Protocols;
using CollectorService.Services;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using KEDA_Share.Repository.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using DeviceStatus = KEDA_Share.Entity.DeviceStatus;
using IProtocolDriver = CollectorService.Protocols.IProtocolDriver;
using PointResult = KEDA_Share.Entity.PointResult;

namespace CollectorService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IWorkstationProvider _workstationProvider;
    private readonly IDeviceStatusRepository _deviceStatusRepository;
    private readonly IDeviceResultRepository _deviceResultRepository;
    private readonly IMqttPublishManager _mqttPublishManager;

    private CancellationTokenSource? _collectCts;
    private long _lastTimestamp = 0;
    private readonly int _maxRetry;

    // 用于管理所有采集任务
    private readonly ConcurrentDictionary<string, Task> _collectorTasks = new();

    public Worker(ILogger<Worker> logger, IWorkstationProvider workstationProvider, IDeviceStatusRepository deviceStatusRepository, IDeviceResultRepository deviceResultRepository, IConfiguration config, IMqttPublishManager mqttPublishManager)
    {
        _logger = logger;
        _workstationProvider = workstationProvider;
        _deviceStatusRepository = deviceStatusRepository;
        _deviceResultRepository = deviceResultRepository;
        _maxRetry = config.GetValue<int>("Collector:MaxRetry", 1);
        _mqttPublishManager = mqttPublishManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _workstationProvider.StartAsync(stoppingToken);

        if (_workstationProvider.Current == null && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("等待工作站配置加载...");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var ws = _workstationProvider.Current;

            if (ws == null)
            {
                _logger.LogWarning("采集时, 工作站为空, 15秒后重试...");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                continue;
            }

            if (ws.Timestamp != _lastTimestamp)
            {
                _logger.LogInformation("工作站配置已更新，正在取消当前的任务...");
                _collectCts?.Cancel();

                // 等待所有采集任务完成并清理
                if (_collectorTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(_collectorTasks.Values);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "采集任务取消时发生异常");
                    }
                    _collectorTasks.Clear();
                }

                _collectCts = new();
                _lastTimestamp = ws.Timestamp;
                await Task.Delay(3000, stoppingToken);
                _logger.LogInformation("重新开始采集...");

                await StartAllCollectorsAsync(ws, _collectCts.Token);
            }

            await Task.Delay(10, stoppingToken); // 建议适当延长间隔，减少CPU空转
        }
    }

    private async Task StartAllCollectorsAsync(Workstation ws, CancellationToken token)
    {
        foreach (var protocol in ws.Protocols)
        {
            // 唯一标识建议用协议类型+IP+端口或协议ID
            var protocolKey = $"{protocol.ProtocolType}_{protocol.GetHashCode()}";

            if (!Enum.TryParse<KEDA_Share.Enums.ProtocolType>(protocol.ProtocolType, out var protocolType))
            {
                _logger.LogWarning($"工作站存在协议 {protocol.ProtocolType} 未实现");
                continue;
            }

            if (protocol.Remark == "控制")
                continue;

            var task = Task.Run(async () =>
            {
                IProtocolDriver? driver = null;
                driver = ProtocolDriverFactory.Create(protocolType);
                try
                {
                    if (driver == null)
                    {
                        _logger.LogWarning($"协议驱动未实现: {protocol.ProtocolType}");
                        return;
                    }

                    while (!token.IsCancellationRequested)
                    {
                        var protocolResult = new ProtocolResult()
                        {
                            ProtocolId = protocol.ProtocolID,
                        };

                        List<EquipmentDto> equipments = [];

                        foreach (var dev in protocol.Devices)
                        {
                            var equipmentDto = new EquipmentDto()
                            {
                                Id = dev.EquipmentID,
                                Name = dev.EquipmentName,
                                EquipmentType = dev.Type == "设备" ? EquipmentType.Equipment : EquipmentType.Instrument,
                                Parameters = [.. dev.Points.Select(p => new ParameterDto
                                {
                                    Label = p.Label,
                                    DataType = Enum.Parse<KEDA_CommonV2.Enums.DataType>(p.DataType),
                                })]
                            };

                            equipments.Add(equipmentDto);

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

                            var devResult = new DeviceResult
                            {
                                DevId = dev.EquipmentID,
                                PointResults = []
                            };

                            var deviceStopwatch = Stopwatch.StartNew();

                            if(protocolType == KEDA_Share.Enums.ProtocolType.MySqlOnlyOneAddress 
                                || protocolType == KEDA_Share.Enums.ProtocolType.Api 
                                || protocolType == KEDA_Share.Enums.ProtocolType.ApiWithOnlyOneAddress)
                                devResult = await driver.ReadAsync(protocol, dev, token);
                            else
                            {
                                foreach (var point in dev.Points)
                                {
                                    var pointStatus = deviceStatus.PointStatuses.First(ps => ps.Label == point.Label);
                                    var pointStopwatch = Stopwatch.StartNew();

                                    var pointResult = new PointResult
                                    {
                                        Label = point.Label,
                                        DataType = Enum.Parse<KEDA_Share.Enums.DataType>(point.DataType),
                                    };
                                    try
                                    {
                                        var result = await driver.ReadAsync(protocol, dev, point, token);

                                        int retryCount = 0;
                                        while (result?.Value == null && retryCount < _maxRetry)
                                        {
                                            _logger.LogWarning($"{dev.EquipmentID}的{point.Label}第一次读取失败，进行第二次读取");
                                            result = await driver.ReadAsync(protocol, dev, point, token);
                                            retryCount++;
                                        }

                                        object? finalValue = result?.Value;
                                        if (finalValue != null && !string.IsNullOrWhiteSpace(point.Change) && ExpressionHelper.IsNumericType(finalValue))
                                        {
                                            try
                                            {
                                                var x = Convert.ToDouble(finalValue);
                                                finalValue = ExpressionHelper.Eval(point.Change, x);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, $"表达式计算失败: {point.Change}, 原值: {result?.Value}");
                                                finalValue = result?.Value;
                                            }
                                        }

                                        pointResult.Result = finalValue;

                                        devResult?.PointResults?.Add(pointResult);

                                        pointStatus.Status = PointReadResult.Success;
                                        pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        pointStatus.Message = "采集成功";
                                    }
                                    catch (ProtocolFailedException ex)
                                    {
                                        _logger.LogError(ex, $"协议连接失败: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
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
                                        _logger.LogError(ex, $"协议异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
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
                                        _logger.LogError(ex, $"点读取失败: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
                                        pointStatus.Status = PointReadResult.Failed;
                                        pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        pointStatus.Message = ex.Message;
                                    }
                                    catch (PointException ex)
                                    {
                                        _logger.LogError(ex, $"点异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
                                        pointStatus.Status = PointReadResult.Exception;
                                        pointStatus.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        pointStatus.Message = ex.Message;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"未知异常: 协议={protocol.ProtocolType}, 设备={dev.EquipmentName}, 点={point.Label}");
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
                                deviceStatus.Message = "全部采集点成功";
                            }
                            else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Failed))
                            {
                                deviceStatus.Status = DevStatus.Exception;
                                deviceStatus.Message = "存在采集点失败";
                            }
                            else if (deviceStatus.PointStatuses.Any(ps => ps.Status == PointReadResult.Exception))
                            {
                                deviceStatus.Status = DevStatus.Exception;
                                deviceStatus.Message = "存在采集点异常";
                            }

                            await _deviceStatusRepository.UpsertAsync(deviceStatus, token);
                            await _deviceResultRepository.AddAsync(devResult, token);

                            var sourcePointResults = devResult.PointResults ?? [];
                            var equipmentResult = new EquipmentResult
                            {
                                EquipmentId = dev.EquipmentID,
                                PointResults = [.. sourcePointResults
                                    .Select(pr => new KEDA_CommonV2.Model.PointResult
                                    {
                                        Label = pr.Label ?? string.Empty,
                                        Value = pr.Result,
                                    })]
                            };

                            protocolResult.EquipmentResults.Add(equipmentResult);
                        }

                        protocolResult.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                       
                        await _mqttPublishManager.ProcessDataAsync(protocolResult, equipments, token); //把协议结果转换，清洗,发布

                        int cycleMs = 1000;
                        if (int.TryParse(protocol.CollectCycle, out var ms) && ms > 0)
                            cycleMs = ms;

                        await Task.Delay(cycleMs, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"协议采集任务异常: {protocol.ProtocolType}");
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
