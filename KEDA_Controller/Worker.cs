using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using SqlSugar;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Channels;

namespace KEDA_Controller;
public class Worker : BackgroundService
{
    private readonly IProtocolConfigProvider _configProvider;//获取协议配置的服务
    private readonly IProtocolTaskManager _taskManager;//任务管理服务
    private readonly IWriteTaskManager _writeTaskManager;//写任务管理服务
    private DateTime _lastConfigTime;//配置最新的时间
    private readonly ILogger<Worker> _logger;//日志

    public Worker(IProtocolConfigProvider configProvider, IProtocolTaskManager taskManager, IWriteTaskManager writeTaskManager, ILogger<Worker> logger)
    {
        _configProvider = configProvider;
        _taskManager = taskManager;
        _writeTaskManager = writeTaskManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _writeTaskManager.InitSubscribeAsync(stoppingToken);//初始化MQTT订阅触发时需要执行的逻辑，写任务入列。触发订阅时，生产者会入列
        await Task.Delay(1000, stoppingToken);
        await _writeTaskManager.StartConsumerAsync(stoppingToken);//启动写任务消费，事件驱动，有写任务到Channel，消费者就会立即处理

        while (!stoppingToken.IsCancellationRequested)
        {
            var latestConfig = await _configProvider.GetLatestConfigAsync(stoppingToken);//获取最新的json配置

            if (latestConfig == null)
            {
                _logger.LogWarning("最新协议配置为空,五秒后重试...");
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            if (_configProvider.IsConfigChanged(latestConfig, _lastConfigTime))//如果时间发生更改，则停止所有读任务再执行所有读任务
            {
                _logger.LogInformation("检测到新配置，重启采集任务 ...");
                await _taskManager.StopAllAsync(stoppingToken);
                _lastConfigTime = latestConfig.SaveTime;
                await _taskManager.StartAllAsync(latestConfig, stoppingToken);
            }

            await Task.Delay(5000, stoppingToken);//5秒检查一次配置是否发生更改
        }
    }
}
//public class Worker : BackgroundService//拆分，
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly SqlSugarClient _db;
//    private readonly int _pointCooldownMs;
//    private readonly IMqttPublishService _mqttPublishService;
//    private readonly IMqttSubscribeService _mqttSubscribeService;
//    private DateTime _lastConfigTime;
//    private readonly JsonSerializerOptions options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
//    //协议ID -> 读取任务的 CancellationTokenSource 用于独立管理每个协议的采集任务生命周期
//    private readonly ConcurrentDictionary<string, CancellationTokenSource> _protocolReadCts = [];
//    private readonly ConcurrentDictionary<string, Task> _protocolReadTasks = [];  //协议ID -> 读取任务
//    private readonly Channel<WriteTaskEntity> _writeChannel = Channel.CreateUnbounded<WriteTaskEntity>();//写任务通道
//    private readonly ConcurrentDictionary<string, IProtocolDriver> _drivers = []; //协议ID -> 协议对象
//    private static readonly HashSet<ProtocolType> ProtocolsNeedCooldown =
//    [
//        ProtocolType.DLT6452007OverTcp,
//        ProtocolType.ModbusRtuOverTcp,
//        ProtocolType.CJT188OverTcp_2004,
//        ProtocolType.FxSerialOverTcp,
//        ProtocolType.DLT6452007Serial,
//        ProtocolType.ModbusRtu,
//        ProtocolType.ModbusRtuSerial,
//    ];

//    public Worker(ILogger<Worker> logger, SqlSugarClient db, IConfiguration config, IMqttPublishService mqttPublishService, IMqttSubscribeService mqttSubscribeService)
//    {
//        _logger = logger;
//        _db = db;
//        _pointCooldownMs = config.GetValue("Collector:PointCooldownMs", 500);
//        _mqttPublishService = mqttPublishService;
//        _mqttSubscribeService = mqttSubscribeService;
//    }

//    //sqlite纯协议采集配置更新怎么办？把采集任务弃元，然后每5秒检查一次最新的配置时间是否改变，改变则停止当前的采集任务，开始新的采集任务
//    //如果数据库连接失败，表不存在，SQL语法错误等，则抛出异常然后5秒后继续看是否异常
//    /// <summary>
//    /// 后台主循环
//    /// 1. 启动写任务消费者（事件驱动写优先）
//    /// 2. 每5秒检查协议配置是否变化，变化则重启所有采集任务
//    /// </summary>
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _ = _mqttSubscribeService.StartAsync(TriggerWriteTaskAsync, stoppingToken);

//        await Task.Delay(1000, stoppingToken);

//        _ = Task.Run(() => WriteTaskConsumerAsync(stoppingToken), stoppingToken);//启动写任务消费，事件驱动，有写任务到Channel，消费者就会立即处理

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            ProtocolConfig? latestConfig = null;
//            try
//            {
//                latestConfig = await _db.Queryable<ProtocolConfig>()
//                    .OrderByDescending(x => x.Id)
//                    .FirstAsync(stoppingToken);//获得最新的协议配置
//            }
//            catch (Exception ex)//数据库异常，抛出
//            {
//                _logger.LogError(ex, "查询最新协议配置时发生异常，5秒后重试...");
//                await Task.Delay(5000, stoppingToken);
//                continue;
//            }

//            if (latestConfig == null)//如果查不到或者为空，等待五秒后再查一次
//            {
//                _logger.LogWarning("最新协议配置为空,五秒后重试...");
//                await Task.Delay(5000, stoppingToken);
//                continue;
//            }

//            if (latestConfig.SaveTime != _lastConfigTime)
//            {
//                _logger.LogInformation("检测到新配置，重启采集任务 ...");

//                // 停止所有协议的读取任务并释放资源
//                await StopAllProtocolReadTasksAsync();

//                _lastConfigTime = latestConfig.SaveTime;
//                await StartAllProtocolReadTasksAsync(latestConfig, stoppingToken);
//            }

//            await Task.Delay(5000, stoppingToken);
//        }
//    }

//    #region 启动读任务
//    /// <summary>
//    /// 停止所有协议的采集任务并释放资源
//    /// 用于配置变更或系统重启场景
//    /// </summary>
//    private async Task StopAllProtocolReadTasksAsync()
//    {
//        var stopTasks = new List<Task>();
//        var protocolIds = _protocolReadCts.Keys.ToList();

//        foreach (var protocolId in protocolIds)
//        {
//            if (_protocolReadCts.TryRemove(protocolId, out var cts))
//            {
//                cts.Cancel();
//                if (_protocolReadTasks.TryRemove(protocolId, out var task))
//                {
//                    // 直接等待任务完成，不需要创建新任务
//                    stopTasks.Add(Task.Run(async () =>
//                    {
//                        try { await task; }
//                        catch (OperationCanceledException) { }
//                    }));
//                }
//                // 统一在最后释放资源
//            }
//        }

//        await Task.WhenAll(stopTasks);

//        // 统一释放所有资源
//        foreach (var protocolId in protocolIds)
//        {
//            if (_protocolReadCts.TryRemove(protocolId, out var cts))
//                cts.Dispose();
//            _drivers.TryRemove(protocolId, out var driver);
//            (driver as IDisposable)?.Dispose();
//        }
//    }

//    /// <summary>
//    /// 启动所有协议的采集任务
//    /// 每个协议独立Task，互不影响
//    /// </summary>
//    private async Task StartAllProtocolReadTasksAsync(ProtocolConfig config, CancellationToken stoppingToken)
//    {
//        #region 初始化协议列表：反序列化是否成功，协议列表数量是否为空或0
//        //反序列化ConfigJson为ProtocolEntity
//        List<ProtocolEntity>? protocolList = null;
//        try
//        {
//            protocolList = JsonSerializer.Deserialize<List<ProtocolEntity>>(config.ConfigJson, options);
//        }
//        catch (Exception ex)//协议列表反序列化失败，直接返回
//        {
//            _logger.LogError(ex, "ConfigJson 反序列化为协议列表失败" + ex.Message);
//            return;
//        }

//        _logger.LogInformation($"最新协议配置已读取: {config.ConfigJson}");
//        #endregion

//        //处理协议列表为空或数量为0
//        if (protocolList == null || protocolList.Count == 0)
//        {
//            _logger.LogError($"最新协议配置已读取: {config.ConfigJson},但是协议列表为空或数量是0");
//            return;
//        }

//        foreach (var protocol in protocolList)
//        {
//            var cts = new CancellationTokenSource();
//            _protocolReadCts[protocol.ProtocolID] = cts;
//            _protocolReadTasks[protocol.ProtocolID] = Task.Run(() => ProtocolReadLoop(protocol, cts.Token), cts.Token);
//        }

//        await Task.CompletedTask;
//    }

//    /// <summary>
//    /// 协议采集任务主循环
//    /// 负责点位数据采集、压缩、存库、MQTT发布
//    /// </summary>
//    private async Task ProtocolReadLoop(ProtocolEntity protocol, CancellationToken token)
//    {
//        var driver = ProtocolDriverFactory.CreateDriver(protocol.ProtocolType);//初始化协议连接对象

//        if (driver == null)
//        {
//            _logger.LogWarning($"协议驱动未实现: {protocol.ProtocolType}");
//            return;//跳出当前lamba表达式，不会跳出StartCollect方法,继续protocolList的下一个协议
//        }

//        _drivers[protocol.ProtocolID] = driver;// 把协议驱动放在字典中，让写任务调用

//        while (!token.IsCancellationRequested)
//        {
//            var protocolResult = new ProtocolResult { ProtocolId = protocol.ProtocolID };//协议结果
//            protocolResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//            var protocolSw = Stopwatch.StartNew();//协议读取计时器
//            protocolResult.ReadIsSuccess = true;//假设协议是读取成功的
//            foreach (var dev in protocol.Devices)//读取地址值
//            {
//                var deviceResult = new DeviceResult() { EquipmentId = dev.EquipmentId };//设备结果
//                var deviceSw = Stopwatch.StartNew();//设备读取计时器
//                deviceResult.ReadIsSuccess = true;//假设本设备是读取成功的
//                deviceResult.StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");//设备读取开始时间
//                foreach (var point in dev.Points)
//                {
//                    var pointSw = Stopwatch.StartNew();
//                    var pointResult = new PointResult { Address = point.Address, Label = point.Label, DataType = point.DataType };
//                    try
//                    {
//                        var result = await driver.ReadAsync(protocol, point, token);
//                        if (result != null) pointResult = result;
//                    }
//                    catch (ProtocolWhenConnFailedException ex)     //协议连接失败，同时是设备和点的信息
//                    {
//                        _logger.LogError($"协议连接失败,已释放连接，下次将自动重连。信息:{ex.Message}");
//                        deviceResult.ReadIsSuccess = false;
//                        deviceResult.ErrorMsg = ex.Message;
//                        (driver as IDisposable)?.Dispose(); // 释放连接，下次自动重连
//                        break; // 跳过当前设备
//                    }
//                    catch (ProtocolIsNullWhenReadException ex)   //读取是协议为空，同时是设备和点的信息
//                    {
//                        _logger.LogError($"读取时发现协议为空，已释放连接，下次将自动重连。信息:{ex.Message}");
//                        deviceResult.ReadIsSuccess = false;
//                        deviceResult.ErrorMsg = ex.Message;
//                        (driver as IDisposable)?.Dispose(); // 释放连接，下次自动重连
//                        break; // 跳过当前设备
//                    }
//                    catch (NotSupportedException ex)//跳过当前点
//                    {
//                        _logger.LogError($"采集点的类型不支持，信息:{ex.Message}");
//                        pointResult.ReadIsSuccess = false;
//                        pointResult.ErrorMsg = ex.Message;
//                    }
//                    catch (ProtocolDefaultException ex)//跳过当前点
//                    {
//                        _logger.LogError($"读取时协议默认异常，信息:{ex.Message}");
//                        pointResult.ReadIsSuccess = false;
//                        pointResult.ErrorMsg = ex.Message;
//                    }
//                    catch (Exception ex)//处理捕获的异常，跳过当前点
//                    {
//                        _logger.LogError($"读取时默认异常，信息:{ex.Message}");
//                        pointResult.ReadIsSuccess = false;
//                        pointResult.ErrorMsg = ex.Message;
//                    }
//                    finally
//                    {
//                        pointSw.Stop();
//                        pointResult.ElapsedMs = pointSw.ElapsedMilliseconds;
//                        deviceResult.PointResults.Add(pointResult);
//                    }

//                    if (ProtocolsNeedCooldown.Contains(protocol.ProtocolType)) //如果是串口或串口相关协议，需要冷却500毫秒
//                        await Task.Delay(_pointCooldownMs, token);
//                }

//                // 如果设备级异常，补全所有点的失败结果
//                if (!deviceResult.ReadIsSuccess)
//                {
//                    deviceResult.PointResults.Clear();
//                    foreach (var point in dev.Points)
//                    {
//                        deviceResult.PointResults.Add(new PointResult
//                        {
//                            Address = point.Address,
//                            Label = point.Label,
//                            ReadIsSuccess = false,
//                            Value = null,
//                            ErrorMsg = deviceResult.ErrorMsg
//                        });
//                    }
//                }
//                else
//                {
//                    deviceResult.ReadIsSuccess = true;
//                    deviceResult.ErrorMsg = string.Empty;
//                }

//                // 设备统计字段和时间充实
//                deviceSw.Stop();
//                deviceResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//                deviceResult.ElapsedMs = deviceSw.ElapsedMilliseconds;
//                deviceResult.TotalPoints = deviceResult.PointResults.Count;
//                deviceResult.SuccessPoints = deviceResult.PointResults.Count(p => p.ReadIsSuccess);
//                deviceResult.FailedPoints = deviceResult.TotalPoints - deviceResult.SuccessPoints;

//                protocolResult.DeviceResults.Add(deviceResult);
//            }

//            // 设备采集结束后，充实协议结果
//            protocolSw.Stop();
//            protocolResult.EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//            protocolResult.ElapsedMs = protocolSw.ElapsedMilliseconds;
//            protocolResult.TotalDevices = protocolResult.DeviceResults.Count;
//            protocolResult.SuccessDevices = protocolResult.DeviceResults.Count(d => d.ReadIsSuccess);
//            protocolResult.FailedDevices = protocolResult.TotalDevices - protocolResult.SuccessDevices;
//            protocolResult.TotalPoints = protocolResult.DeviceResults.Sum(d => d.TotalPoints);
//            protocolResult.SuccessPoints = protocolResult.DeviceResults.Sum(d => d.SuccessPoints);
//            protocolResult.FailedPoints = protocolResult.TotalPoints - protocolResult.SuccessPoints;
//            protocolResult.ReadIsSuccess = protocolResult.FailedDevices == 0 && protocolResult.FailedPoints == 0;
//            protocolResult.ErrorMsg = protocolResult.ReadIsSuccess ? string.Empty : "部分设备或点采集失败";

//            // 统计所有点是否都失败
//            bool allPointsFailed = protocolResult.FailedPoints == protocolResult.TotalPoints && protocolResult.TotalPoints > 0;
//            if (allPointsFailed)
//            {
//                if (_drivers.TryGetValue(protocol.ProtocolID, out var driver1))
//                {
//                    (driver1 as IDisposable)?.Dispose();
//                    _drivers.TryRemove(protocol.ProtocolID, out _);
//                    _logger.LogWarning($"协议[{protocol.ProtocolID}]所有点采集失败，已释放连接，下次将自动重连。");
//                }
//            }

//            var data = JsonSerializer.Serialize(protocolResult, options);// 序列化为 JSON

//            await _mqttPublishService.PublishAsync("workstation/" + protocol.ProtocolID, data, token);

//            ////GZip压缩编码,转换成字节数组，发布并持久化存储到SQLite中
//            //byte[] compressedByteArray;
//            //using (var ms = new MemoryStream())
//            //{
//            //    using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, true))
//            //    using (var writer = new StreamWriter(gzip))
//            //    {
//            //        writer.Write(data);
//            //    }
//            //    compressedByteArray = ms.ToArray();
//            //}

//            //var protocolData = new ProtocolData
//            //{
//            //    ProtocolID = protocol.ProtocolID,
//            //    Payload = compressedByteArray,
//            //    SaveTime = DateTime.Now,
//            //};

//            ////await _db.Insertable(protocolData).ExecuteCommandAsync(token);//存到数据库然后让处理中心读取处理,不存数据库了，只发MQTT

//            //await _mqttPublishService.PublishAsync("workstation/" + protocol.ProtocolID, compressedByteArray, token);//发布到MQTT服务器，做单点测试,设备无关，工作站无关

//            await Task.Delay(10000, token);
//        }
//    }
//    #endregion

//    #region 写任务优先
//    /// <summary>
//    /// 写任务事件消费者
//    /// Channel事件驱动，实时响应写任务，保证写优先
//    /// </summary>
//    private async Task WriteTaskConsumerAsync(CancellationToken stoppingToken)  //写任务事件消费者
//    {
//        //从队列中获取写任务,Channel 是事件驱动的，只要有写任务被推送（如通过 await _writeChannel.Writer.WriteAsync(...)），消费者会立即处理。
//        await foreach (var writeTask in _writeChannel.Reader.ReadAllAsync(stoppingToken))
//        {
//            await HandleProtocolWriteTaskAsync(writeTask, stoppingToken);
//        }
//    }

//    /// <summary>
//    /// 写任务处理
//    /// 1. 停止目标协议采集任务
//    /// 2. 执行写操作
//    /// 3. 查询最新配置，重启采集任务
//    /// </summary>
//    private async Task HandleProtocolWriteTaskAsync(WriteTaskEntity writeTask, CancellationToken stoppingToken)  //写任务处理
//    {
//        var protocolId = writeTask.Protocol.ProtocolID;
//        //停止该协议的读取任务
//        if (_protocolReadCts.TryRemove(protocolId, out var cts))
//        {
//            cts.Cancel();
//            try
//            {
//                if (_protocolReadTasks.TryRemove(protocolId, out var task))
//                    await task;
//            }
//            catch (OperationCanceledException) { }
//            finally
//            {
//                cts.Dispose();
//                _drivers.TryRemove(protocolId, out var driver);
//                (driver as IDisposable)?.Dispose();
//            }
//            await Task.Delay(200, stoppingToken);
//        }

//        //执行写任务
//        await DoProtocolWriteTaskAsync(protocolId, writeTask, stoppingToken);

//        #region 查询最新的采集配置
//        ProtocolConfig? latestConfig = null;
//        try
//        {
//            latestConfig = await _db.Queryable<ProtocolConfig>()
//                .OrderByDescending(x => x.Id)
//                .FirstAsync(stoppingToken);
//        }
//        catch (Exception ex)//数据库异常，抛出
//        {
//            _logger.LogError(ex, "查询最新协议配置时发生异常");
//            // 可选：等待一段时间后重试
//            await Task.Delay(5000, stoppingToken);
//            return;
//        }

//        if (latestConfig == null)//如何查不到或者为空，等待五秒后再查一次
//        {
//            _logger.LogWarning("未找到最新的协议配置");
//            await Task.Delay(5000, stoppingToken);
//            return;
//        }
//        #endregion

//        //写完后重启该协议的读取任务
//        List<ProtocolEntity>? protocolList = null;
//        try
//        {
//            protocolList = JsonSerializer.Deserialize<List<ProtocolEntity>>(latestConfig.ConfigJson);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "配置反序列化失败");
//            return;
//        }

//        if (protocolList == null) return;

//        var protocol = protocolList?.FirstOrDefault(p => p.ProtocolID == protocolId);
//        if (protocol != null)
//        {
//            var newCts = new CancellationTokenSource();
//            _protocolReadCts[protocolId] = newCts;
//            _protocolReadTasks[protocolId] = Task.Run(() => ProtocolReadLoop(protocol, newCts.Token), newCts.Token);
//        }
//    }

//    /// <summary>
//    /// 协议写操作
//    /// 优先使用已存在的驱动对象，若无则新建驱动
//    /// </summary>
//    private async Task DoProtocolWriteTaskAsync(string protocolID, WriteTaskEntity writeTask, CancellationToken stoppingToken)
//    {
//        if (_drivers.TryGetValue(protocolID, out var driver))
//            await driver.WriteAsync(writeTask, stoppingToken);
//        else
//        {
//            var driverNew = ProtocolDriverFactory.CreateDriver(writeTask.Protocol.ProtocolType);
//            if (driverNew == null)
//            {
//                _logger.LogWarning($"协议驱动未实现: {writeTask.Protocol.ProtocolType}");
//                return;
//            }

//            try
//            {
//                await driverNew.WriteAsync(writeTask, stoppingToken);
//            }
//            finally
//            {
//                (driverNew as IDisposable)?.Dispose();
//            }
//        }
//    }

//    // 外部事件/消息触发写任务（如MQTT、API、配置变更等调用此方法）
//    public async Task TriggerWriteTaskAsync(WriteTaskEntity writeTask)
//    {
//        await _writeChannel.Writer.WriteAsync(writeTask);
//    }
//    #endregion
//}