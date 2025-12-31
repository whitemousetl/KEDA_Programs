using Dm.util;
using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using Microsoft.Extensions.Options;
using NetTaste;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_Controller.Services;
public class ProtocolTaskManager : IProtocolTaskManager
{
    private readonly JsonSerializerOptions options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly int _pointCooldownMs;
    private readonly ILogger<ProtocolTaskManager> _logger;
    private readonly IMqttPublishService _mqttPublishService;
    //协议ID -> 读取任务的 CancellationTokenSource 用于独立管理每个协议的采集任务生命周期
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _protocolReadCts = [];
    private readonly ConcurrentDictionary<string, Task> _protocolReadTasks = [];  //协议ID -> 读取任务
    private readonly ConcurrentDictionary<string, IProtocolDriver> _drivers = []; //协议ID -> 协议对象
    private static readonly HashSet<ProtocolType> ProtocolsNeedCooldown =
  [
        ProtocolType.DLT6452007OverTcp,
        ProtocolType.CJT188OverTcp_2004,
        ProtocolType.FxSerialOverTcp,
        ProtocolType.DLT6452007Serial,
        ProtocolType.ModbusRtu,
        ProtocolType.ModbusRtuSerial,
        ProtocolType.ModbusRtuOverTcp,
    ];

    public ProtocolTaskManager(ILogger<ProtocolTaskManager> logger, IConfiguration config, IMqttPublishService mqttPublishService)
    {
        _logger = logger;
        _pointCooldownMs = config.GetValue("Collector:PointCooldownMs", 500);
        _mqttPublishService = mqttPublishService;
    }

    /// <summary>
    /// 启动所有协议的采集任务
    /// 每个协议独立Task，互不影响
    /// </summary>
    public async Task StartAllAsync(ProtocolConfig config, CancellationToken token)
    {
        #region 初始化协议列表：反序列化是否成功，协议列表数量是否为空或0
        //反序列化ConfigJson为ProtocolEntity
        List<WorkstationEntity>? protocolList = null;
        try
        {
            protocolList = JsonSerializer.Deserialize<List<WorkstationEntity>>(config.ConfigJson, options);
        }
        catch (Exception ex)//协议列表反序列化失败，直接返回
        {
            _logger.LogError(ex, "ConfigJson 反序列化为协议列表失败" + ex.Message);
            return;
        }

        _logger.LogInformation($"最新协议配置已读取: {config.ConfigJson}");
        #endregion

        //处理协议列表为空或数量为0
        if (protocolList == null || protocolList.Count == 0)
        {
            _logger.LogError($"最新协议配置已读取: {config.ConfigJson},但是协议列表为空或数量是0");
            return;
        }

        foreach (var protocol in protocolList)
        {
            if (!protocol.Devices?.Any() ?? true || protocol.Devices.All(d => !d.Points?.Any() ?? true)) continue; //过滤掉没有设备或所有设备都没有采集点的协议
            var cts = new CancellationTokenSource();
            _protocolReadCts[protocol.ProtocolID] = cts;
            _protocolReadTasks[protocol.ProtocolID] = Task.Run(() => ProtocolReadLoop(protocol, cts.Token), cts.Token);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 协议采集任务主循环
    /// 负责点位数据采集、压缩、存库、MQTT发布
    /// </summary>
    private async Task ProtocolReadLoop(WorkstationEntity protocol, CancellationToken token)
    {
        CreateDriver(protocol, out IProtocolDriver? driver);
        if (driver == null) return;

        while (!token.IsCancellationRequested)
        {
            var protocolResult = new ProtocolResult { ProtocolId = protocol.ProtocolID };//协议结果
            protocolResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");//协议开始读取时间
            var protocolSw = Stopwatch.StartNew();//协议读取计时器
            protocolResult.ReadIsSuccess = true;//假设协议是读取成功的
            foreach (var dev in protocol.Devices)//读取地址值
            {
                var deviceResult = await ReadDeviceAsync(dev, driver, protocol, token);

                // 如果设备级异常，补全所有点的失败结果
                UpdatePointResultIfDevError(dev, deviceResult);

                protocolResult.DeviceResults.Add(deviceResult);
            }

            protocolSw.Stop();
            CompleteProtocolStatistics(protocol, protocolResult, protocolSw); // 所有设备采集结束后，充实协议结果

            var data = JsonSerializer.Serialize(protocolResult, options);// 序列化为 JSON

            await _mqttPublishService.PublishAsync("edge/" + protocol.ProtocolID, data, token);

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
            //    ProtocolID = protocol.ProtocolID,
            //    Payload = compressedByteArray,
            //    SaveTime = DateTime.Now,
            //};

            ////await _db.Insertable(protocolData).ExecuteCommandAsync(token);//存到数据库然后让处理中心读取处理,不存数据库了，只发MQTT

            //await _mqttPublishService.PublishAsync("workstation/" + protocol.ProtocolID, compressedByteArray, token);//发布到MQTT服务器，做单点测试,设备无关，工作站无关


            // CollectCycle 为 0 时，避免忙等占满 CPU：最小延迟或 Yield 一下
            var delayMs = protocol.CollectCycle;
            if (delayMs <= 0) delayMs = 1; // 最小 1ms，或改为 await Task.Yield();

            await Task.Delay(delayMs, token);
            //await Task.Delay(protocol.CollectCycle, token);
        }

    }

    private void CreateDriver(WorkstationEntity protocol, out IProtocolDriver? driver)
    {
        driver = ProtocolDriverFactory.CreateDriver(protocol.ProtocolType, _mqttPublishService);
        if (driver == null)
        {
            _logger.LogWarning($"协议驱动未实现: {protocol.ProtocolType}");
            return;//跳出当前lamba表达式，不会跳出StartCollect方法,继续protocolList的下一个协议
        }

        _drivers[protocol.ProtocolID] = driver;// 把协议驱动放在字典中，让写任务调用
    }

    private async Task<DeviceResult> ReadDeviceAsync(DeviceEntity dev, IProtocolDriver driver, WorkstationEntity protocol, CancellationToken token)
    {
        var deviceResult = new DeviceResult() { EquipmentId = dev.EquipmentId };//设备结果
        var deviceSw = Stopwatch.StartNew();//设备读取计时器
        deviceResult.ReadIsSuccess = true;//假设本设备是读取成功的
        deviceResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");//设备读取开始时间
        foreach (var point in dev.Points)
        {
            if(point == null) continue;
            if(point.Address == "VirtualPoint")
            {
                // 虚拟点，直接给默认结果
                var virtualPointResult = new ProtocolResult
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
            var pointResult = new ProtocolResult { Address = point.Address, Label = point.Label, DataType = point.DataType };
            try
            {
                var result = await driver.ReadAsync(protocol, dev.EquipmentId, point, token);
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

            if (ProtocolsNeedCooldown.Contains(protocol.ProtocolType)) //如果是串口或串口相关协议，需要冷却500毫秒
                await Task.Delay(_pointCooldownMs, token);
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

    private static void UpdatePointResultIfDevError(DeviceEntity dev, DeviceResult deviceResult)
    {
        if (!deviceResult.ReadIsSuccess)
        {
            deviceResult.PointResults.Clear();
            foreach (var point in dev.Points)
            {
                deviceResult.PointResults.Add(new ProtocolResult
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

    private void CompleteProtocolStatistics(WorkstationEntity protocol, ProtocolResult protocolResult, Stopwatch protocolSw)
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
            if (_drivers.TryGetValue(protocol.ProtocolID, out var driver1))
            {
                (driver1 as IDisposable)?.Dispose();
                _drivers.TryRemove(protocol.ProtocolID, out _);
                _logger.LogWarning($"协议[{protocol.ProtocolID}]所有点采集失败，已释放连接，下次将自动重连。");
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
    public async Task RestartProtocolAsync(string protocolId, WorkstationEntity protocol, CancellationToken token)
    {
        await StopProtocolAsync(protocolId, token);
        var newCts = new CancellationTokenSource();
        _protocolReadCts[protocolId] = newCts;
        _protocolReadTasks[protocolId] = Task.Run(() => ProtocolReadLoop(protocol, newCts.Token), newCts.Token);   
    }

    public ConcurrentDictionary<string, IProtocolDriver> GetDrivers() =>  _drivers;
}
