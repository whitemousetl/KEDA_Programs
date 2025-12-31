using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Controller.Interfaces;
using KEDA_Common.CustomException;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using ProtocolType = KEDA_Common.Enums.ProtocolType;

namespace KEDA_Controller.Protocols.Special;

[ProtocolType(ProtocolType.FJ60W)]
public class FJ60WDriver : IProtocolDriver
{
    private readonly IMqttPublishService _mqttPublishService;
    private TcpClient? _client;
    private NetworkStream? _stream;

    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private volatile bool _receiveStarted = false;

    // 点位最新结果缓存（key 建议使用 Address，若 Address 为空可用 Label）
    private readonly ConcurrentDictionary<string, ProtocolResult> _latestPointResults = new();

    public FJ60WDriver(IMqttPublishService mqttPublishService)
    {
        _mqttPublishService = mqttPublishService;
    }

    public string GetProtocolName() => "FJ60W";

    // ReadAsync 只返回缓存，不做网络读取
    public Task<ProtocolResult?> ReadAsync(WorkstationEntity protocol, string devId, PointEntity point, CancellationToken token)
    {
        // 确保接收循环已经运行（若未启动且有协议信息，尝试连接并启动）
        // 注意：这里不做阻塞式连接；连接失败时由接收循环异常处理
        _ = EnsureReceiveLoopStartedAsync(protocol, token);

        var key = GetPointKey(point);
        if (_latestPointResults.TryGetValue(key, out var cached))
        {
            // 返回一个浅拷贝，避免调用方修改内部缓存
            var snapshot = new ProtocolResult
            {
                DataType = cached.DataType,
                Label = cached.Label,
                Address = cached.Address,
                Value = cached.Value,
                ReadIsSuccess = cached.ReadIsSuccess,
                ErrorMsg = cached.ErrorMsg,
                ElapsedMs = cached.ElapsedMs,
                Metadata = new Dictionary<string, object>(cached.Metadata ?? [])
            };
            return Task.FromResult<ProtocolResult?>(snapshot);
        }

        // 没有缓存时返回失败
        var result = new ProtocolResult
        {
            DataType = point.DataType,
            Label = point.Label,
            Address = point.Address,
            ReadIsSuccess = false,
            ErrorMsg = "尚未收到设备数据",
            Value = null,
            ElapsedMs = 0,
            Metadata = []
        };
        return Task.FromResult<ProtocolResult?>(result);
    }

    public async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
    {
        bool result = false;

        var protocol = new WorkstationEntity
        {
            IPAddress = writeTask.IPAddress,
            ProtocolPort = writeTask.ProtocolPort,
            ReceiveTimeOut = writeTask.ReceiveTimeOut,
            ConnectTimeOut = writeTask.ConnectTimeOut,
        };

        try
        {
            await EnsureConnectedAsync(protocol, token);

            if (writeTask.WriteDevice == null) return false;
            var points = writeTask.WriteDevice.WritePoints;

            string msg;
            if (points.Length == 1)
                msg = points[0].Value;
            else
                msg = string.Join("\\,", points.Select(p => p.Value));

            msg = "SM" + msg + "\r\n";

            byte[] data = Encoding.UTF8.GetBytes(msg);

            await _stream!.WriteAsync(data, 0, data.Length, token);
            await _stream.FlushAsync(token);

            var buffer = new byte[256];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(protocol.ReceiveTimeOut);

            int read = 0;
            try
            {
                read = await _stream.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                result = false;
            }
            if (read > 0)
            {
                string str = Encoding.ASCII.GetString(buffer, 0, read);
                result = str == "SMX\r\n";
            }
        }
        catch (Exception ex)
        {
            throw new ProtocolDefaultException("FJ60W 协议 Write 操作失败，" + ex.Message, ex);
        }

        return result;
    }

    // 启动接收循环（仅在需要时启动一次）
    private async Task EnsureReceiveLoopStartedAsync(WorkstationEntity protocol, CancellationToken token)
    {
        if (_receiveStarted) return;

        // 标记已启动，防止并发重复启动
        _receiveStarted = true;
        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var loopToken = _receiveCts.Token;

        try
        {
            await EnsureConnectedAsync(protocol, loopToken);
        }
        catch
        {
            // 连接失败不抛出给调用方，接收循环里会重试/或由上层策略处理
        }

        _receiveTask = Task.Run(() => ReceiveLoop(protocol, loopToken), loopToken);
    }

    // 后台循环：持续读取服务器数据，解析并更新缓存
    private async Task ReceiveLoop(WorkstationEntity protocol, CancellationToken token)
    {
        var buffer = new byte[2048];
        var sb = new StringBuilder();

        while (!token.IsCancellationRequested)
        {
            try
            {
                // 如果断开则尝试重连
                if (_client == null || !_client.Connected || _stream == null)
                {
                    DisposeConnection();
                    await EnsureConnectedAsync(protocol, token);
                }

                // 读取流（行模式）
                int read = await _stream!.ReadAsync(buffer, 0, buffer.Length, token);
                if (read <= 0)
                {
                    // 对端关闭或无数据，短暂等待再试
                    await Task.Delay(50, token);
                    continue;
                }

                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                // 处理多行
                while (true)
                {
                    var text = sb.ToString();
                    var idx = text.IndexOf("\r\n", StringComparison.Ordinal);
                    if (idx < 0) break; // 不够一行

                    var line = text.Substring(0, idx);
                    sb.Remove(0, idx + 2);

                    // 示例：如果收到 "MK" 表示本次帧有效，后续应继续解析具体字段。
                    // 你的真实协议可能是："MK,<point1>=<val1>,<point2>=<val2>..."
                    // 这里做两种处理：
                    // 1) 如果只是一个触发标志："MK"，我们把所有点标记为成功或更新某些通用值
                    // 2) 如果有数据帧，调用解析函数按点更新缓存

                    if (line == "MK")
                    {
                        // 简化处理：收到 MK 标志，给缓存中的点打成功标记，或填入一个时间戳值
                        MarkAllKnownPointsSuccess();
                    }
                    else
                    {
                        // 假定其他行是数据帧，按你的协议格式解析并更新点缓存
                        ParseFrameAndUpdatePoints(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // 网络异常，短暂等待后重试
                await Task.Delay(200, token);
            }
        }
    }

    private string GetPointKey(PointEntity point)
        => string.IsNullOrWhiteSpace(point.Address) ? point.Label : point.Address;

    // 收到 MK 时，示例地把当前已知点都标记为成功（实际按你的协议填充值）
    private void MarkAllKnownPointsSuccess()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        foreach (var kv in _latestPointResults)
        {
            var pr = kv.Value;
            var updated = new ProtocolResult
            {
                DataType = pr.DataType,
                Label = pr.Label,
                Address = pr.Address,
                // 示例：把值设置为当前时间戳，或保留原值
                Value = pr.Value ?? now,
                ReadIsSuccess = true,
                ErrorMsg = string.Empty,
                ElapsedMs = 0,
                Metadata = pr.Metadata ?? new Dictionary<string, object>()
            };
            _latestPointResults[kv.Key] = updated;
        }
    }

    // 按你的实际协议解析数据帧，把每个点的值更新到缓存
    private void ParseFrameAndUpdatePoints(string frame)
    {
        // TODO: 按真实帧格式解析
        // 例如：frame = "MK,addr001=123.4,addr002=ON"
        // 这里给出一个非常简单的示例解析：

        if (string.IsNullOrWhiteSpace(frame)) return;
        var parts = frame.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // 假设第一段是 "MK" 或其它帧头
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;

            var key = kv[0];      // 地址或标签
            var val = kv[1];      // 值字符串

            _latestPointResults.AddOrUpdate(
                key,
                _ => new ProtocolResult
                {
                    DataType = DataType.String, // 或根据点定义设置类型
                    Label = key,
                    Address = key,
                    Value = val,
                    ReadIsSuccess = true,
                    ErrorMsg = string.Empty,
                    ElapsedMs = 0,
                    Metadata = []
                },
                updateValueFactory: (_, existing) =>
                {
                    existing.Value = val;
                    existing.ReadIsSuccess = true;
                    existing.ErrorMsg = string.Empty;
                    existing.ElapsedMs = 0;
                    return existing;
                }
            );
        }
    }

    // 为了让 ProtocolTaskManager 的首次读不全是失败，你可以在设备加载点列表时先初始化缓存
    public void InitializePointsCache(DeviceEntity device)
    {
        foreach (var point in device.Points)
        {
            var key = GetPointKey(point);
            _latestPointResults.TryAdd(key, new ProtocolResult
            {
                DataType = point.DataType,
                Label = point.Label,
                Address = point.Address,
                ReadIsSuccess = false,
                ErrorMsg = "尚未收到设备数据",
                Value = null,
                ElapsedMs = 0,
                Metadata = new Dictionary<string, object>()
            });
        }
    }

    private async Task EnsureConnectedAsync(WorkstationEntity protocol, CancellationToken token)
    {
        if (_client != null && _client.Connected && _stream != null)
            return;

        DisposeConnection();

        _client = new ();
        _client.ReceiveTimeout = protocol.ReceiveTimeOut;
        _client.SendTimeout = protocol.ConnectTimeOut;

        var connectTask = _client.ConnectAsync(protocol.IPAddress, protocol.ProtocolPort);

        if (await Task.WhenAny(connectTask, Task.Delay(protocol.ConnectTimeOut, token)) != connectTask)
            throw new ProtocolWhenConnFailedException("FJ60W TCP 连接超时", null);

        if (!_client.Connected)
            throw new ProtocolWhenConnFailedException("FJ60W TCP 连接失败", null);

        _stream = _client.GetStream();
    }

    private void DisposeConnection()
    {
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        _stream = null;
        _client = null;
    }

    public void Dispose()
    {
        try { _receiveCts?.Cancel(); } catch { }
        try { _receiveTask?.Wait(200); } catch { }
        _receiveCts?.Dispose();
        DisposeConnection();
        GC.SuppressFinalize(this);
    }
}









//using KEDA_Common.Interfaces;
//using KEDA_Common.Model;
//using KEDA_Common.Entity;
//using KEDA_Common.Enums;
//using KEDA_Controller.Interfaces;
//using KEDA_Common.CustomException;
//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;
//using ProtocolType = KEDA_Common.Enums.ProtocolType;
//using System.Collections.Concurrent;

//namespace KEDA_Controller.Protocols.Special;

//[ProtocolType(ProtocolType.FJ60W)]
//public class FJ60WDriver : IProtocolDriver
//{
//    private readonly IMqttPublishService _mqttPublishService;
//    private TcpClient? _client;
//    private NetworkStream? _stream;

//    private CancellationTokenSource? _pollingCts;
//    private Task? _pollingTask;
//    private bool _pollingStarted = false;

//    private readonly ConcurrentDictionary<string, PointResult> _lastResults = new();

//    public FJ60WDriver(IMqttPublishService mqttPublishService)
//    {
//        _mqttPublishService = mqttPublishService;
//    }

//    #region === IProtocolDriver 必须实现的方法 ===

//    public string GetProtocolName() => "FJ60W";

//    public Task<PointResult?> ReadAsync(ProtocolEntity protocol, string devId, PointEntity point, CancellationToken token)
//    {
//        if (_lastResults.TryGetValue(devId, out var result))
//        {
//            // 返回克隆数据
//            return Task.FromResult<PointResult?>(new PointResult
//            {
//                Label = result.Label,
//                Address = result.Address,
//                Value = result.Value,
//                ReadIsSuccess = result.ReadIsSuccess,
//                ErrorMsg = "",
//                ElapsedMs = result.ElapsedMs,
//                DataType = point.DataType,
//                Metadata = new Dictionary<string, object>(result.Metadata)
//            });
//        }

//        // 没有新数据就返回 null → ReadIsSuccess=false
//        return Task.FromResult<PointResult?>(null);
//    }

//    private async Task PollIncomingAsync(CancellationToken token)
//    {
//        var buffer = new byte[256];

//        while (!token.IsCancellationRequested)
//        {
//            int read = await _stream.ReadAsync(buffer, token);
//            if (read <= 0) continue;

//            string msg = Encoding.ASCII.GetString(buffer, 0, read);

//            if (msg.StartsWith("MK"))
//            {
//                // 解析成点位值
//                var pointResult = new PointResult
//                {
//                    Label = "MK状态",
//                    Address = "MK",
//                    Value = true,
//                    ReadIsSuccess = true,
//                    ElapsedMs = 0
//                };

//                // 通常协议是 1 台设备，可以把 devId 存在 connect 参数里
//                _lastResults["DEFAULT"] = pointResult;
//            }
//        }
//    }


//    public async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
//    {
//        bool result = false;

//        var protocol = new ProtocolEntity
//        {
//            IPAddress = writeTask.IPAddress,
//            ProtocolPort = writeTask.ProtocolPort,
//            ReceiveTimeOut = writeTask.ReceiveTimeOut,
//            ConnectTimeOut = writeTask.ConnectTimeOut,
//        };

//        try
//        {
//            // 初始化 TCP 连接
//            await EnsureConnectedAsync(protocol, token);

//            if (writeTask.WriteDevice == null) return false;
//            var points = writeTask.WriteDevice.WritePoints;

//            // 拼接指令
//            string msg;
//            if (points.Length == 1)
//                msg = points[0].Value;
//            else
//                msg = string.Join("\\,", points.Select(p => p.Value));

//            msg = "SM" + msg + "\r\n";

//            byte[] data = Encoding.UTF8.GetBytes(msg);

//            // 发送数据
//            await _stream!.WriteAsync(data, 0, data.Length, token);
//            await _stream.FlushAsync(token);

//            // 读取返回
//            var buffer = new byte[256];
//            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
//            cts.CancelAfter(protocol.ReceiveTimeOut); // 设置超时时间（毫秒）

//            int read = 0;
//            try
//            {
//                read = await _stream.ReadAsync(buffer, cts.Token);
//            }
//            catch (OperationCanceledException)
//            {
//                // 超时处理
//                throw new TimeoutException("NetworkStream 读取超时");
//            }
//            if (read > 0)
//            {
//                string str = Encoding.ASCII.GetString(buffer, 0, read);
//                result = str == "SMX\r\n";
//            }
//        }
//        catch (Exception ex)
//        {
//            throw new ProtocolDefaultException("FJ60W 协议 Write 操作失败，" + ex.Message, ex);
//        }

//        return result;
//    }
//    #endregion

//    #region === TCP 连接管理 ===

//    private async Task EnsureConnectedAsync(ProtocolEntity protocol, CancellationToken token)
//    {
//        if (_client != null && _client.Connected)
//            return;

//        DisposeConnection();

//        _client = new TcpClient();
//        _client.ReceiveTimeout = protocol.ReceiveTimeOut;
//        _client.SendTimeout = protocol.ConnectTimeOut;

//        var connectTask = _client.ConnectAsync(protocol.IPAddress, protocol.ProtocolPort);

//        if (await Task.WhenAny(connectTask, Task.Delay(protocol.ConnectTimeOut, token)) != connectTask)
//            throw new ProtocolWhenConnFailedException("FJ60W TCP 连接超时", null);

//        if (!_client.Connected)
//            throw new ProtocolWhenConnFailedException("FJ60W TCP 连接失败", null);

//        _stream = _client.GetStream();
//    }
//    #endregion

//    #region === 释放资源 ===

//    private void DisposeConnection()
//    {
//        try { _stream?.Close(); } catch { }
//        try { _client?.Close(); } catch { }

//        _stream = null;
//        _client = null;
//    }

//    public void Dispose()
//    {
//        DisposeConnection();
//        GC.SuppressFinalize(this);
//    }
//    #endregion
//}






//using HslCommunication.Profinet.Freedom;
//using KEDA_Common.CustomException;
//using KEDA_Common.Entity;
//using KEDA_Common.Enums;
//using KEDA_Common.Interfaces;
//using KEDA_Common.Model;
//using KEDA_Controller.Base;
//using KEDA_Controller.Services;
//using System.Text;
//using System.Text.Json;

//namespace KEDA_Controller.Protocols.Tcp;
//[ProtocolType(ProtocolType.FJ60W)]
//public class FJ60WDriver : HslTcpBaseProtocolDriver<FreedomTcpNet> //方嘉激光箱码
//{
//    public FJ60WDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
//    {
//    }

//    protected override FreedomTcpNet CreateConnection(ProtocolEntity protocol, CancellationToken token)
//    {
//        var conn = new FreedomTcpNet(protocol.IPAddress, protocol.ProtocolPort)
//        {
//            ReceiveTimeOut = protocol.ReceiveTimeOut,
//            ConnectTimeOut = protocol.ConnectTimeOut,
//        };
//        if (conn.CommunicationPipe is HslCommunication.Core.Pipe.PipeTcpNet pipe)
//            pipe.IsPersistentConnection = true;
//        return conn;
//    }

//    #region 写方法
//    private bool _pollingStarted = false;
//    public override async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
//    {
//        bool result = false;

//        //初始化_conn
//        var protocol = new ProtocolEntity
//        {
//            IPAddress = writeTask.IPAddress,
//            ProtocolPort = writeTask.ProtocolPort,
//            ReceiveTimeOut = writeTask.ReceiveTimeOut,
//            ConnectTimeOut = writeTask.ConnectTimeOut,
//        };

//        try
//        {
//            if (_conn == null)
//            {
//                _conn = CreateConnection(protocol, token);
//                await OnConnectionInitializedAsync(token);
//            }

//            if (writeTask.WriteDevice == null) return false;

//            var points = writeTask.WriteDevice.WritePoints;

//            var msg = string.Empty;

//            if (points.Length == 1)
//                msg = points[0].Value;
//            else
//                msg = string.Join("\\,", points.Select(p => p.Value));

//            msg = "SM" + msg + "\r\n";

//            var data = Encoding.UTF8.GetBytes(msg);

//            var res = await _conn.ReadFromCoreServerAsync(data);
//            if (res.IsSuccess)
//            {
//                var str = Encoding.ASCII.GetString(res.Content);
//                result = str == "SMX\r\n";
//            }
//        }
//        catch (Exception ex) when (
//        ex is ProtocolWhenConnFailedException ||//连接plc失败异常
//        ex is ProtocolIsNullWhenWriteException ||//当写入时协议为空异常
//        ex is NotSupportedException) //不支持的类型异常
//        {
//            // 直接抛出已知的自定义异常
//            throw;
//        }
//        catch (Exception ex)
//        {
//            // 统一处理未知异常
//            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);//抛出默认异常
//        }

//        //只在第一次或轮询任务异常时执行
//        if (!_pollingStarted || (_pollingTask != null && _pollingTask.IsFaulted))
//        {
//            StartPolling(writeTask.WriteDevice.DeviceId);
//            _pollingStarted = true;
//        }

//        return result;
//    }
//    #endregion

//    private CancellationTokenSource? _pollingCts;
//    private Task? _pollingTask;

//    private void StartPolling(string deviceId)
//    {
//        // 停止旧的轮询
//        _pollingCts?.Cancel();
//        _pollingTask?.Wait();

//        _pollingCts = new CancellationTokenSource();
//        var token = _pollingCts.Token;

//        _pollingTask = Task.Run(async () =>
//        {
//            bool printFlag = false;

//            while (!token.IsCancellationRequested)
//            {
//                try
//                {
//                    if (_conn == null)
//                    {
//                        await Task.Delay(100);
//                        continue;
//                    }

//                    var res = _conn.ReadFromCoreServer(Array.Empty<byte>());
//                    if (res.IsSuccess && res.Content != null)
//                    {
//                        var str = Encoding.ASCII.GetString(res.Content);
//                        if (str.Contains("MK\r\n")) printFlag = true;
//                        else printFlag = false;
//                    }

//                    var payload = JsonSerializer.Serialize(new
//                    {
//                        DeviceId = deviceId,
//                        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
//                        是否打印 = printFlag
//                    });

//                    await _mqttPublishService.PublishAsync("laser/parameters", payload, token);
//                }
//                catch (Exception)
//                {
//                    // 忽略异常，继续轮询
//                }
//                await Task.Delay(100, token);
//            }
//        }, token);
//    }

//    private void StopPolling()
//    {
//        _pollingCts?.Cancel();
//        _pollingTask?.Wait();
//        _pollingCts = null;
//        _pollingTask = null;
//    }
//}
