using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace KEDA_ControllerV2.Services;

public class ProtocolTaskManager : IProtocolTaskManager
{
    private readonly JsonSerializerOptions options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILogger<ProtocolTaskManager> _logger;
    private readonly IMqttPublishManager _mqttPublishManager;
    private readonly IDeviceNotificationService _deviceNotificationService;
    private readonly ConcurrentDictionary<string, DateTime> _lastMonitorTimes = new();

    //协议ID -> 读取任务的 CancellationTokenSource 用于独立管理每个协议的采集任务生命周期
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _protocolReadCts = [];

    private readonly ConcurrentDictionary<string, Task> _protocolReadTasks = [];  //协议ID -> 读取任务
    private readonly ConcurrentDictionary<string, IProtocolDriver> _drivers = []; //协议ID -> 协议对象
    private readonly IWorkstationConfigProvider _workstationConfigProvider;

    public ProtocolTaskManager(ILogger<ProtocolTaskManager> logger, IWorkstationConfigProvider workstationConfigProvider, IMqttPublishManager mqttPublishManager, IDeviceNotificationService deviceNotificationService)
    {
        _logger = logger;
        _workstationConfigProvider = workstationConfigProvider;
        _mqttPublishManager = mqttPublishManager;
        _deviceNotificationService = deviceNotificationService;
    }

    /// <summary>
    /// 启动所有协议的采集任务
    /// 每个协议独立Task，互不影响
    /// </summary>
    public async Task StartAllAsync(CancellationToken token)
    {
        var ws = await _workstationConfigProvider.GetLatestWrokstationAsync(token);

        if (ws == null)
        {
            _logger.LogError($"查询的工作站为空");
            return;
        }
        var protocolList = ws.Protocols;

        //处理协议列表为空或数量为0
        if (protocolList == null || protocolList.Count == 0)
        {
            _logger.LogError($"协议列表为空或数量是0");
            return;
        }

        foreach (var protocol in protocolList)
        {
            if (!protocol.Equipments?.Any() ?? true || protocol.Equipments.All(d => !d.Parameters?.Any() ?? true)) continue; //过滤掉没有设备或所有设备都没有采集点的协议
            //网口或串口协议
            if(protocol.InterfaceType == InterfaceType.COM || protocol.InterfaceType == InterfaceType.LAN)
            {
                var cts = new CancellationTokenSource();
                _protocolReadCts[protocol.Id] = cts;
                _protocolReadTasks[protocol.Id] = Task.Run(() => ProtocolReadLoop(protocol, cts.Token), cts.Token);
            }
            else if(protocol.InterfaceType == InterfaceType.API || protocol.InterfaceType == InterfaceType.DATABASE)
            {
                var cts = new CancellationTokenSource();
                _protocolReadCts[protocol.Id] = cts;
                _protocolReadTasks[protocol.Id] = Task.Run(() => ApiAndDblReadLoop(protocol, cts.Token), cts.Token);
            }
        }

        await Task.CompletedTask;
    }

    private async Task ApiAndDblReadLoop(ProtocolDto protocol, CancellationToken token)
    {
        CreateDriver(protocol, out IProtocolDriver? driver);
        if (driver == null) return;

        var delayMs = protocol.CollectCycle;
        if (delayMs <= 0) delayMs = 1; // 最小 1ms，或改为 await Task.Yield();

        while (!token.IsCancellationRequested)
        {
            var protocolResult = await driver.ReadAsync(protocol, token);

            // 主处理流程，必须等待
            if(protocolResult == null)
            {
                await Task.Delay(delayMs, token);
                continue;
            }
            await _mqttPublishManager.ProcessDataAsync(protocolResult, protocol, token); //把协议结果转换，清洗,发布

            // ======= 设备状态监控频率限制（每个 protocol 限制 1 分钟一次） =======
            var now = DateTime.UtcNow;

            if (!_lastMonitorTimes.TryGetValue(protocol.Id, out var lastTime) ||
                (now - lastTime).TotalSeconds >= 60)
            {
                // 更新执行时间，避免并发重复触发
                _lastMonitorTimes[protocol.Id] = now;

                _ = Task.Run(async () =>
                {
                    await _deviceNotificationService.MonitorDeviceStatusAsync(protocolResult, token); // 监控设备状态，发布MQTT设备主题
                }, token);
            }

            var data = JsonSerializer.Serialize(protocolResult, options);// 序列化为 JSON

            //await _mqttPublishService.PublishAsync("edge/" + protocol.ProtocolId, data, token);

            ////GZip压缩编码,转换成字节数组，发布并持久化存储到SQLite中
            //byte[] compressedByteArray;
            //using (var ms = new MemoryStream())
            //{
            //    using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, true))
            //    using (var writer = new StreamWriter(gzip))
            //    {
            //        writer.Write(data);
            //    }
            //    compressedByteArray = ms.ToArray();
            //}

            //var protocolData = new ProtocolData
            //{
            //    ProtocolId = protocol.ProtocolId,
            //    Payload = compressedByteArray,
            //    SaveTime = DateTime.Now,
            //};

            ////await _db.Insertable(protocolData).ExecuteCommandAsync(token);//存到数据库然后让处理中心读取处理,不存数据库了，只发MQTT

            //await _mqttPublishService.PublishAsync("workstation/" + protocol.ProtocolId, compressedByteArray, token);//发布到MQTT服务器，做单点测试,设备无关，工作站无关

            // CollectCycle 为 0 时，避免忙等占满 CPU：最小延迟或 Yield 一下

            await Task.Delay(delayMs, token);
            //await Task.Delay(protocol.CollectCycle, token);
        }
    }

    /// <summary>
    /// 协议采集任务主循环
    /// 负责点位数据采集、压缩、存库、MQTT发布
    /// </summary>
    private async Task ProtocolReadLoop(ProtocolDto protocol, CancellationToken token)
    {
        CreateDriver(protocol, out IProtocolDriver? driver);
        if (driver == null) return;

        while (!token.IsCancellationRequested)
        {
            var protocolResult = new ProtocolResult { ProtocolId = protocol.Id };//协议结果
            protocolResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");//协议开始读取时间
            var protocolSw = Stopwatch.StartNew();//协议读取计时器
            protocolResult.ReadIsSuccess = true;//假设协议是读取成功的
            foreach (var dev in protocol.Equipments)//读取地址值
            {
                var deviceResult = await ReadDeviceAsync(dev, driver, protocol, token);

                // 如果设备级异常，补全所有点的失败结果
                UpdatePointResultIfDevError(dev, deviceResult);

                protocolResult.DeviceResults.Add(deviceResult);
            }

            protocolSw.Stop();
            CompleteProtocolStatistics(protocol, protocolResult, protocolSw); // 所有设备采集结束后，充实协议结果

            // 主处理流程，必须等待
            await _mqttPublishManager.ProcessDataAsync(protocolResult, protocol, token); //把协议结果转换，清洗,发布

            // ======= 设备状态监控频率限制（每个 protocol 限制 1 分钟一次） =======
            var now = DateTime.UtcNow;

            if (!_lastMonitorTimes.TryGetValue(protocol.Id, out var lastTime) ||
                (now - lastTime).TotalSeconds >= 60)
            {
                // 更新执行时间，避免并发重复触发
                _lastMonitorTimes[protocol.Id] = now;

                _ = Task.Run(async () =>
                {
                    await _deviceNotificationService.MonitorDeviceStatusAsync(protocolResult, token); // 监控设备状态，发布MQTT设备主题
                }, token);
            }

            var data = JsonSerializer.Serialize(protocolResult, options);// 序列化为 JSON

            //await _mqttPublishService.PublishAsync("edge/" + protocol.ProtocolId, data, token);

            ////GZip压缩编码,转换成字节数组，发布并持久化存储到SQLite中
            //byte[] compressedByteArray;
            //using (var ms = new MemoryStream())
            //{
            //    using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, true))
            //    using (var writer = new StreamWriter(gzip))
            //    {
            //        writer.Write(data);
            //    }
            //    compressedByteArray = ms.ToArray();
            //}

            //var protocolData = new ProtocolData
            //{
            //    ProtocolId = protocol.ProtocolId,
            //    Payload = compressedByteArray,
            //    SaveTime = DateTime.Now,
            //};

            ////await _db.Insertable(protocolData).ExecuteCommandAsync(token);//存到数据库然后让处理中心读取处理,不存数据库了，只发MQTT

            //await _mqttPublishService.PublishAsync("workstation/" + protocol.ProtocolId, compressedByteArray, token);//发布到MQTT服务器，做单点测试,设备无关，工作站无关

            // CollectCycle 为 0 时，避免忙等占满 CPU：最小延迟或 Yield 一下
            var delayMs = protocol.CollectCycle;
            if (delayMs <= 0) delayMs = 1; // 最小 1ms，或改为 await Task.Yield();

            await Task.Delay(delayMs, token);
            //await Task.Delay(protocol.CollectCycle, token);
        }
    }

    private void CreateDriver(ProtocolDto protocol, out IProtocolDriver? driver)
    {
        driver = ProtocolDriverFactory.CreateDriver(protocol.ProtocolType);
        if (driver == null)
        {
            _logger.LogWarning($"协议驱动未实现: {protocol.ProtocolType}");
            return;//跳出当前lamba表达式，不会跳出StartCollect方法,继续protocolList的下一个协议
        }

        _drivers[protocol.Id] = driver;// 把协议驱动放在字典中，让写任务调用
    }

    private async Task<DeviceResult> ReadDeviceAsync(EquipmentDto dev, IProtocolDriver driver, ProtocolDto protocol, CancellationToken token)
    {
        var deviceResult = new DeviceResult() { EquipmentId = dev.Id, EquipmentName = dev.Name };//设备结果
        var deviceSw = Stopwatch.StartNew();//设备读取计时器
        deviceResult.ReadIsSuccess = true;//假设本设备是读取成功的
        deviceResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");//设备读取开始时间
        foreach (var point in dev.Parameters)
        {
            if (point == null) continue;
            if (point.Address == "VirtualPoint")
            {
                // 虚拟点，直接给默认结果
                var virtualPointResult = new PointResult
                {
                    Address = point.Address,
                    Label = point.Label,
                    DataType = point.DataType,
                    ReadIsSuccess = true,
                    Value = null, // 或者你需要的默认值
                    ErrorMsg = null!,
                    ElapsedMs = 0
                };
                deviceResult.PointResults.Add(virtualPointResult);
                continue;
            }
            var pointSw = Stopwatch.StartNew();
            var pointResult = new PointResult { Address = point.Address, Label = point.Label, DataType = point.DataType };
            try
            {
                var result = await driver.ReadAsync(protocol, dev.Id, point, token);
                if (result != null) pointResult = result;
            }
            catch (ProtocolWhenConnFailedException ex)     //协议连接失败，同时是设备和点的信息
            {
                _logger.LogError($"协议连接失败,已释放连接，下次将自动重连。信息:{ex.Message}");
                deviceResult.ReadIsSuccess = false;
                deviceResult.ErrorMsg = ex.Message;
                (driver as IDisposable)?.Dispose(); // 释放连接，下次自动重连
                break; // 跳过当前设备
            }
            catch (ProtocolIsNullWhenReadException ex)   //读取是协议为空，同时是设备和点的信息
            {
                _logger.LogError($"读取时发现协议为空，已释放连接，下次将自动重连。信息:{ex.Message}");
                deviceResult.ReadIsSuccess = false;
                deviceResult.ErrorMsg = ex.Message;
                (driver as IDisposable)?.Dispose(); // 释放连接，下次自动重连
                break; // 跳过当前设备
            }
            catch (NotSupportedException ex)//跳过当前点
            {
                _logger.LogError($"采集点的类型不支持，信息:{ex.Message}");
                pointResult.ReadIsSuccess = false;
                pointResult.ErrorMsg = ex.Message;
            }
            catch (ProtocolDefaultException ex)//跳过当前点
            {
                _logger.LogError($"读取时协议默认异常，信息:{ex.Message}");
                pointResult.ReadIsSuccess = false;
                pointResult.ErrorMsg = ex.Message;
            }
            catch (Exception ex)//处理捕获的异常，跳过当前点
            {
                _logger.LogError($"读取时默认异常，信息:{ex.Message}");
                pointResult.ReadIsSuccess = false;
                pointResult.ErrorMsg = ex.Message;
            }
            finally
            {
                pointSw.Stop();
                pointResult.ElapsedMs = pointSw.ElapsedMilliseconds;
                deviceResult.PointResults.Add(pointResult);
            }

            if (SharedConfigHelper.SerialLikeProtocols.Contains(protocol.ProtocolType)) //如果是串口或串口相关协议，需要冷却500毫秒
                await Task.Delay(SharedConfigHelper.CollectorSettings.PointCooldownMs, token);
        }

        // 设备统计字段和时间充实
        deviceSw.Stop();
        deviceResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        deviceResult.ElapsedMs = deviceSw.ElapsedMilliseconds;
        deviceResult.TotalPoints = deviceResult.PointResults.Count;
        deviceResult.SuccessPoints = deviceResult.PointResults.Count(p => p.ReadIsSuccess);
        deviceResult.FailedPoints = deviceResult.TotalPoints - deviceResult.SuccessPoints;

        return deviceResult;
    }

    private static void UpdatePointResultIfDevError(EquipmentDto dev, DeviceResult deviceResult)
    {
        if (!deviceResult.ReadIsSuccess)
        {
            deviceResult.PointResults.Clear();
            foreach (var point in dev.Parameters)
            {
                deviceResult.PointResults.Add(new PointResult
                {
                    Address = point.Address,
                    Label = point.Label,
                    ReadIsSuccess = false,
                    Value = null,
                    ErrorMsg = deviceResult.ErrorMsg
                });
            }

            // 补全统计字段
            deviceResult.TotalPoints = deviceResult.PointResults.Count;
            deviceResult.SuccessPoints = deviceResult.PointResults.Count(p => p.ReadIsSuccess);
            deviceResult.FailedPoints = deviceResult.TotalPoints - deviceResult.SuccessPoints;
        }
    }

    private void CompleteProtocolStatistics(ProtocolDto protocol, ProtocolResult protocolResult, Stopwatch protocolSw)
    {
        protocolResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        protocolResult.ElapsedMs = protocolSw.ElapsedMilliseconds;
        protocolResult.TotalDevices = protocolResult.DeviceResults.Count;
        protocolResult.SuccessDevices = protocolResult.DeviceResults.Count(d => d.ReadIsSuccess);
        protocolResult.FailedDevices = protocolResult.TotalDevices - protocolResult.SuccessDevices;
        protocolResult.TotalPoints = protocolResult.DeviceResults.Sum(d => d.TotalPoints);
        protocolResult.SuccessPoints = protocolResult.DeviceResults.Sum(d => d.SuccessPoints);
        protocolResult.FailedPoints = protocolResult.TotalPoints - protocolResult.SuccessPoints;
        protocolResult.ReadIsSuccess = protocolResult.FailedDevices == 0 && protocolResult.FailedPoints == 0;
        protocolResult.ErrorMsg = protocolResult.ReadIsSuccess ? string.Empty : "部分设备或点采集失败";

        // 统计所有点是否都失败
        bool allPointsFailed = protocolResult.FailedPoints == protocolResult.TotalPoints && protocolResult.TotalPoints > 0;
        if (allPointsFailed)
        {
            if (_drivers.TryGetValue(protocol.Id, out var driver1))
            {
                (driver1 as IDisposable)?.Dispose();
                _drivers.TryRemove(protocol.Id, out _);
                _logger.LogWarning($"协议[{protocol.Id}]所有点采集失败，已释放连接，下次将自动重连。");
            }
        }
    }

    /// <summary>
    /// 停止所有协议的采集任务并释放资源
    /// 用于配置变更或系统重启场景
    /// </summary>
    public async Task StopAllAsync(CancellationToken token)
    {
        var stopTasks = new ConcurrentBag<Task>();
        var protocolIds = _protocolReadCts.Keys.ToList();

        foreach (var protocolId in protocolIds)
        {
            if (_protocolReadCts.TryRemove(protocolId, out var cts))
            {
                cts.Cancel();
                if (_protocolReadTasks.TryRemove(protocolId, out var task))
                {
                    // 直接等待任务完成，不需要创建新任务
                    stopTasks.Add(Task.Run(async () =>
                    {
                        try { await task; }
                        catch (OperationCanceledException) { }
                    }, token));
                }
                // 统一在最后释放资源
            }
        }

        await Task.WhenAll(stopTasks);

        // 统一释放所有资源
        foreach (var protocolId in protocolIds)
        {
            if (_protocolReadCts.TryRemove(protocolId, out var cts))
                cts.Dispose();
            _drivers.TryRemove(protocolId, out var driver);
            (driver as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 执行写任务时停止指定协议的读取
    /// </summary>
    public async Task StopProtocolAsync(string protocolId, CancellationToken token)
    {
        if (_protocolReadCts.TryRemove(protocolId, out var cts))
        {
            cts?.Cancel();
            if (_protocolReadTasks.TryRemove(protocolId, out var task))
                try { await task; } catch (OperationCanceledException) { }
            cts?.Dispose();
            _drivers.TryRemove(protocolId, out var driver);
            (driver as IDisposable)?.Dispose();
            await Task.Delay(200, token);
        }
    }

    /// <summary>
    /// 执行完写操作之后，恢复指定协议的读取
    /// </summary>
    public async Task RestartProtocolAsync(string protocolId, ProtocolDto protocol, CancellationToken token)
    {
        await StopProtocolAsync(protocolId, token);
        var newCts = new CancellationTokenSource();
        _protocolReadCts[protocolId] = newCts;
        _protocolReadTasks[protocolId] = Task.Run(() => ProtocolReadLoop(protocol, newCts.Token), newCts.Token);
    }

    public async Task<bool> RestartAllProtocolsAsync(CancellationToken token)
    {
        try
        {
            await StopAllAsync(token);
            _logger.LogInformation("所有协议采集任务已取消，准备重新启动...");

            await StartAllAsync(token);
            _logger.LogInformation("所有协议采集任务已重新启动完成。");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启所有协议采集任务时发生异常");
            return false;
        }
    }

    public ConcurrentDictionary<string, IProtocolDriver> GetDrivers() => _drivers;
}