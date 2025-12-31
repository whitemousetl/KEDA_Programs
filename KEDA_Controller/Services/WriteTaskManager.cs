using HslCommunication.Core;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KEDA_Controller.Services;
public class WriteTaskManager : IWriteTaskManager
{
    private readonly Channel<WriteTaskEntity> _writeChannel = Channel.CreateUnbounded<WriteTaskEntity>();//写任务通道
    private readonly ILogger<WriteTaskManager> _logger;
    private readonly IProtocolConfigProvider _configProvider;
    private readonly IProtocolTaskManager _protocolTaskManager;
    private readonly IMqttSubscribeService _mqttSubscribeService;
    private readonly IWriteTaskLogService _writeTaskLogService;
    private readonly IMqttPublishService _mqttPublishService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly HashSet<ProtocolType> _serialProtocols; //串口或类串口协议

    public WriteTaskManager(ILogger<WriteTaskManager> logger, IProtocolConfigProvider configProvider, IProtocolTaskManager protocolTaskManager, IMqttSubscribeService mqttSubscribeService, IWriteTaskLogService writeTaskLogService, IMqttPublishService mqttPublishService, IConfiguration configuration)
    {
        _logger = logger;
        _configProvider = configProvider;
        _protocolTaskManager = protocolTaskManager;
        _mqttSubscribeService = mqttSubscribeService;
        _writeTaskLogService = writeTaskLogService;
        _mqttPublishService = mqttPublishService;
        _serialProtocols = configuration
            .GetSection("SerialProtocol")
            .Get<ConcurrentBag<string>>()?
            .Select(x => Enum.TryParse<ProtocolType>(x, out var pt) ? pt : (ProtocolType?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet() ?? [];
    }

    public async Task InitSubscribeAsync(CancellationToken token) //订阅写主题
    {
        var topicHandles = new ConcurrentDictionary<string, Func<WritePointData, CancellationToken, Task>>();
        topicHandles["protocol/write"] = TriggerWriteTaskAsync;
        await _mqttSubscribeService.StartAsync(topicHandles, token);
    }

    /// <summary>
    /// 写任务事件消费者
    /// Channel事件驱动，实时响应写任务，保证写优先
    /// </summary>
    private async Task WriteTaskConsumerAsync(CancellationToken stoppingToken)  //写任务事件消费者
    {
        //从队列中获取写任务,Channel 是事件驱动的，只要有写任务被推送（如通过 await _writeChannel.Writer.WriteAsync(...)），消费者会立即处理。
        await foreach (var writeTask in _writeChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleProtocolWriteTaskAsync(writeTask, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写任务处理异常，已忽略，继续处理后续任务");
                // 可选：写任务失败后是否需要重试或通知
            }
        }
    }

    /// <summary>
    /// 写任务处理
    /// 1. 停止目标协议采集任务
    /// 2. 执行写操作
    /// 3. 查询最新配置，重启采集任务
    /// </summary>
    private async Task HandleProtocolWriteTaskAsync(WriteTaskEntity writeTask, CancellationToken stoppingToken) // 要修改DoProtocolWriteTaskAsync方法，返回true
    {
        var protocolId = writeTask.ProtocolID;
        bool isSuccess = false;
        string msg = string.Empty;

        // 判断是否为串口串行协议
        bool isSerial = _serialProtocols.Contains(writeTask.ProtocolType);

        if (isSerial)
            await _protocolTaskManager.StopProtocolAsync(protocolId, stoppingToken);

        try
        {
            isSuccess = await DoProtocolWriteTaskAsync(protocolId, writeTask, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写任务异常");
            msg = ex.Message;
            isSuccess = false; // 有异常，视为失败
        }
        finally
        {
            var log = new WriteTaskLog
            {
                UUID = writeTask.UUID,
                WriteTaskJson = JsonSerializer.Serialize(writeTask, _jsonOptions),
                DeviceType = writeTask.DeviceType,
                Time = DateTime.Now,
                Msg = msg,
                IsSuccess = isSuccess
            };
            await _writeTaskLogService.AddLogAsync(log);

            // 发布写任务结果到MQTT
            var resultTopic = "protocol/write_result";
            var resultPayload = JsonSerializer.Serialize(log, _jsonOptions);
            await _mqttPublishService.PublishAsync(resultTopic, resultPayload, stoppingToken);

            if (isSerial)
            {
                var protocolEntity = await _configProvider.GetProtocolEntityByProtocolIdAsync(protocolId, stoppingToken);
                if (protocolEntity != null)
                    await _protocolTaskManager.RestartProtocolAsync(protocolId, protocolEntity, stoppingToken);
                else
                    _logger.LogWarning($"未找到ProtocolId={protocolId}对应的协议实体，无法重启采集任务。");
            }
        }
    }

    private readonly ConcurrentDictionary<string, IProtocolDriver> _tcpDriverCache = new();
    /// <summary>
    /// 协议写操作
    /// 优先使用已存在的驱动对象，若无则新建驱动，执行完写任务就释放连接，特殊协议即使驱动不存在也不会执行完写任务就释放连接
    /// </summary>
    private async Task<bool> DoProtocolWriteTaskAsync(string protocolId, WriteTaskEntity writeTask, CancellationToken stoppingToken)
    {
        // 1. 优先查找全局驱动池
        var drivers = _protocolTaskManager.GetDrivers();
        if (drivers.TryGetValue(protocolId, out var driver))
            return await driver.WriteAsync(writeTask, stoppingToken);

        // 2. 查找本地TCP驱动缓存
        if (_tcpDriverCache.TryGetValue(protocolId, out var cachedDriver))
            return await cachedDriver.WriteAsync(writeTask, stoppingToken);

        // 3. 都没有则新建
        var driverNew = ProtocolDriverFactory.CreateDriver(writeTask.ProtocolType, _mqttPublishService);
        if (driverNew == null)
        {
            var msg = $"协议驱动未实现: {writeTask.ProtocolType}";
            _logger.LogWarning(msg);
            throw new NotSupportedException(msg);
        }

        bool isSerial = _serialProtocols.Contains(writeTask.ProtocolType);

        if (isSerial)
        {
            // 串口协议：用完即释放
            try
            {
                return await driverNew.WriteAsync(writeTask, stoppingToken);
            }
            finally
            {
                (driverNew as IDisposable)?.Dispose();
            }
        }
        else
        {
            // 非串口协议：加入本地缓存，后续复用
            _tcpDriverCache.TryAdd(protocolId, driverNew);
            return await driverNew.WriteAsync(writeTask, stoppingToken);
        }
    }

    public Task StartConsumerAsync(CancellationToken token)
    {
        _ = WriteTaskConsumerAsync(token);
        return Task.CompletedTask;
    }

    public async Task TriggerWriteTaskAsync(WritePointData writePointData, CancellationToken token)
    {
        var writeTask = ConvertToWriteTaskEntity(writePointData);
        await _writeChannel.Writer.WriteAsync(writeTask, token);
    }

    // 辅助转换方法
    private WriteTaskEntity ConvertToWriteTaskEntity(WritePointData data)
    {
        WriteDevice? writeDevice = null;
        var firstDevice = data.Devices?.FirstOrDefault();
        if (firstDevice != null)
        {
            writeDevice = new WriteDevice
            {
                DeviceId = firstDevice.EquipmentID,
                InstrumentType = byte.TryParse(data.InstrumentType, out var insType) ? insType : (byte)0,
                AddressStartWithZero = bool.TryParse(data.AddressStartWithZero, out var addrZero) && addrZero,
                WritePoints = firstDevice.Points?.Select(p =>
                {
                    var value = p.Value;
                    if (!string.IsNullOrWhiteSpace(p.Change) && !string.IsNullOrWhiteSpace(value))
                        value = ReverseChangeValue(p.Change, value);
                    return new WritePoint
                    {
                        Label = p.Label,
                        DataType = Enum.TryParse<DataType>(p.DataType, true, out var dt) ? dt : DataType.String,
                        Address = p.Address,
                        StationNo = firstDevice.StationNo,
                        Length = ushort.TryParse(p.Length, out var len) ? len : (ushort)0,
                        Format = Enum.TryParse<DataFormat>(data.Format, true, out var fmt) ? fmt : DataFormat.ABCD,
                        Value = value
                    };
                }).ToArray() ?? []
            };
        }

        return new WriteTaskEntity
        {
            UUID = data.UUID,
            ProtocolID = data.ProtocolID,
            DeviceType = data.DeviceType,
            Interface = Enum.TryParse<ProtocolInterface>(data.Interface, true, out var iface) ? iface : ProtocolInterface.LAN,
            ProtocolType = Enum.TryParse<ProtocolType>(data.ProtocolType, true, out var ptype) ? ptype : ProtocolType.Modbus,
            IPAddress = data.IPAddress,
            Gateway = data.Gateway,
            ProtocolPort = int.TryParse(data.ProtocolPort, out var port) ? port : 0,
            PortName = data.PortName,
            BaudRate = int.TryParse(data.BaudRate, out var baud) ? baud : 0,
            DataBits = int.TryParse(data.DataBits, out var bits) ? bits : 8,
            StopBits = Enum.TryParse<System.IO.Ports.StopBits>(data.StopBits, true, out var stopBits) ? stopBits : System.IO.Ports.StopBits.One,
            Parity = Enum.TryParse<System.IO.Ports.Parity>(data.Parity, true, out var parity) ? parity : System.IO.Ports.Parity.None,
            Remark = data.Remark,
            CollectCycle = int.TryParse(data.CollectCycle, out var cycle) ? cycle : 1000,
            ReceiveTimeOut = int.TryParse(data.ReceiveTimeOut, out var rto) ? rto : 1000,
            ConnectTimeOut = int.TryParse(data.ConnectTimeOut, out var cto) ? cto : 1000,
            WriteDevice = writeDevice
        };
    }

    //private WriteTaskEntity ConvertToWriteTaskEntity(WritePointData data)
    //{
    //    // 设备转换
    //    WriteDevice? writeDevice = null;
    //    var firstDevice = data.Devices?.FirstOrDefault();
    //    if (firstDevice != null)
    //    {
    //        writeDevice = new WriteDevice
    //        {
    //            DeviceId = firstDevice.EquipmentID,
    //            InstrumentType = byte.TryParse(data.InstrumentType, out var insType) ? insType : (byte)0,
    //            AddressStartWithZero = bool.TryParse(data.AddressStartWithZero, out var addrZero) && addrZero,
    //            WritePoints = firstDevice.Points?.Select(p => new WritePoint
    //            {
    //                Label = p.Label,
    //                DataType = Enum.TryParse<DataType>(p.DataType, true, out var dt) ? dt : DataType.String,
    //                Address = p.Address,
    //                StationNo = firstDevice.StationNo,
    //                Length = ushort.TryParse(p.Length, out var len) ? len : (ushort)0,
    //                Format = Enum.TryParse<DataFormat>(data.Format, true, out var fmt) ? fmt : DataFormat.ABCD,
    //                Value = !string.IsNullOrEmpty(p.Value) ? p.Value : (!string.IsNullOrEmpty(p.Value) ? p.Value : string.Empty)
    //            }).ToArray() ?? []
    //        };
    //    }

    //    return new WriteTaskEntity
    //    {
    //        UUID = data.UUID,
    //        ProtocolID = data.ProtocolID,
    //        DeviceType = data.DeviceType,
    //        Interface = Enum.TryParse<ProtocolInterface>(data.Interface, true, out var iface) ? iface : ProtocolInterface.LAN,
    //        ProtocolType = Enum.TryParse<ProtocolType>(data.ProtocolType, true, out var ptype) ? ptype : ProtocolType.Modbus,
    //        IPAddress = data.IPAddress,
    //        Gateway = data.Gateway,
    //        ProtocolPort = int.TryParse(data.ProtocolPort, out var port) ? port : 0,
    //        PortName = data.PortName,
    //        BaudRate = int.TryParse(data.BaudRate, out var baud) ? baud : 0,
    //        DataBits = int.TryParse(data.DataBits, out var bits) ? bits : 8,
    //        StopBits = Enum.TryParse<System.IO.Ports.StopBits>(data.StopBits, true, out var stopBits) ? stopBits : System.IO.Ports.StopBits.One,
    //        Parity = Enum.TryParse<System.IO.Ports.Parity>(data.Parity, true, out var parity) ? parity : System.IO.Ports.Parity.None,
    //        Remark = data.Remark,
    //        CollectCycle = int.TryParse(data.CollectCycle, out var cycle) ? cycle : 1000,
    //        ReceiveTimeOut = int.TryParse(data.ReceiveTimeOut, out var rto) ? rto : 1000,
    //        ConnectTimeOut = int.TryParse(data.ConnectTimeOut, out var cto) ? cto : 1000,
    //        WriteDevice = writeDevice
    //    };
    //}

    // 反向一元一次方程求原始值
    private string ReverseChangeValue(string change, string value)
    {
        // 支持形如 "x*a+b"、"x*a"、"x+b"、"x-a"、"x/a"、"x*a-b" 等
        // 只支持一元一次，且变量为x
        try
        {
            double y = Convert.ToDouble(value);
            change = change.Replace(" ", "");
            if (change.StartsWith("x*") && !change.Contains("+") && !change.Contains("-"))
            {
                // x*a
                var a = double.Parse(change.Substring(2));
                return (y / a).ToString();
            }
            else if (change.StartsWith("x/") && !change.Contains("+") && !change.Contains("-"))
            {
                // x/a
                var a = double.Parse(change.Substring(2));
                return (y * a).ToString();
            }
            else if (change.StartsWith("x+") && !change.Contains("*") && !change.Contains("/"))
            {
                // x+b
                var b = double.Parse(change.Substring(2));
                return (y - b).ToString();
            }
            else if (change.StartsWith("x-") && !change.Contains("*") && !change.Contains("/"))
            {
                // x-b
                var b = double.Parse(change.Substring(2));
                return (y + b).ToString();
            }
            else if (change.StartsWith("x*") && change.Contains("+"))
            {
                // x*a+b
                var arr = change.Substring(2).Split('+');
                var a = double.Parse(arr[0]);
                var b = double.Parse(arr[1]);
                return ((y - b) / a).ToString();
            }
            else if (change.StartsWith("x*") && change.Contains("-"))
            {
                // x*a-b
                var arr = change.Substring(2).Split('-');
                var a = double.Parse(arr[0]);
                var b = double.Parse(arr[1]);
                return ((y + b) / a).ToString();
            }
            // 其它复杂表达式可扩展
        }
        catch
        {
            // 解析失败则返回原值
        }
        return value;
    }
}
