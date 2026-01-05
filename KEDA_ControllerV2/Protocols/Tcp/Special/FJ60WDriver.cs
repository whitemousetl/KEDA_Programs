using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using ProtocolType = KEDA_CommonV2.Enums.ProtocolType;

namespace KEDA_ControllerV2.Protocols.Tcp.Special;

[ProtocolType(ProtocolType.FJ60W)]
public class FJ60WDriver : IProtocolDriver
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private volatile bool _receiveStarted = false;

    // 点位最新结果缓存（key 建议使用 Address，若 Address 为空可用 Label）
    private readonly ConcurrentDictionary<string, PointResult> _latestPointResults = new();

    private string _protocolName = string.Empty;

    public FJ60WDriver() => _protocolName = GetProtocolName();

    public string GetProtocolName() => "FJ60W";

    // ReadAsync 只返回缓存，不做网络读取
    public Task<PointResult?> ReadAsync(ProtocolDto protocol, string equipmentId, ParameterDto point, CancellationToken token)
    {
        // 确保接收循环已经运行（若未启动且有协议信息，尝试连接并启动）
        // 注意：这里不做阻塞式连接；连接失败时由接收循环异常处理
        _ = EnsureReceiveLoopStartedAsync(protocol, token);

        var key = GetPointKey(point);
        if (_latestPointResults.TryGetValue(key, out var cached))
        {
            // 返回一个浅拷贝，避免调用方修改内部缓存
            var snapshot = new PointResult
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
            return Task.FromResult<PointResult?>(snapshot);
        }

        // 没有缓存时返回失败
        var result = new PointResult
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
        return Task.FromResult<PointResult?>(result);
    }

    public async Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token)
    {
        if (writeTask.Protocol is LanProtocolDto lanProtocol)
        {
            bool result = false;

            try
            {
                await EnsureConnectedAsync(lanProtocol, token);

                if (lanProtocol.Equipments == null) return false;
                var points = lanProtocol.Equipments[0].Parameters;

                string msg;
                if (points.Count == 1)
                    msg = points[0].Value;
                else
                    msg = string.Join("\\,", points.Select(p => p.Value));

                msg = "SM" + msg + "\r\n";

                byte[] data = Encoding.UTF8.GetBytes(msg);

                await _stream!.WriteAsync(data, 0, data.Length, token);
                await _stream.FlushAsync(token);

                var buffer = new byte[256];
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(lanProtocol.ReceiveTimeOut);

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
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
    }

    // 启动接收循环（仅在需要时启动一次）
    private async Task EnsureReceiveLoopStartedAsync(ProtocolDto protocol, CancellationToken token)
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
    private async Task ReceiveLoop(ProtocolDto protocol, CancellationToken token)
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

    private string GetPointKey(ParameterDto point)
        => string.IsNullOrWhiteSpace(point.Address) ? point.Label : point.Address;

    // 收到 MK 时，示例地把当前已知点都标记为成功（实际按你的协议填充值）
    private void MarkAllKnownPointsSuccess()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        foreach (var kv in _latestPointResults)
        {
            var pr = kv.Value;
            var updated = new PointResult
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
                _ => new PointResult
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
    public void InitializePointsCache(EquipmentDto equipment)
    {
        foreach (var point in equipment.Parameters)
        {
            var key = GetPointKey(point);
            _latestPointResults.TryAdd(key, new PointResult
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

    private async Task EnsureConnectedAsync(ProtocolDto protocol, CancellationToken token)
    {
        if (protocol is LanProtocolDto lanProtocol)
        {
            if (_client != null && _client.Connected && _stream != null)
                return;

            DisposeConnection();

            _client = new();
            _client.ReceiveTimeout = protocol.ReceiveTimeOut;
            _client.SendTimeout = protocol.ConnectTimeOut;

            var connectTask = _client.ConnectAsync(lanProtocol.IpAddress, lanProtocol.ProtocolPort);

            if (await Task.WhenAny(connectTask, Task.Delay(protocol.ConnectTimeOut, token)) != connectTask)
                throw new ProtocolWhenConnFailedException("FJ60W TCP 连接超时", null);

            if (!_client.Connected)
                throw new ProtocolWhenConnFailedException("FJ60W TCP 连接失败", null);

            _stream = _client.GetStream();
        }
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

    public Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}