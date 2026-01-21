using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using KEDA_Controller.Interfaces;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Special;

[ProtocolType(KEDA_Common.Enums.ProtocolType.BottomImageF1970)]
public class BottomImageF1970Driver : IProtocolDriver
{
    protected readonly string _protocolName;
    private const int DefaultChunkSize = 2048;
    private const int ReadBufferSize = 256;
    private const int DefaultFinalTimeoutMs = 30000;
    private const int DefaultTailGraceMs = 2000;
    private const int PerChunkDelayMs = 50;

    public BottomImageF1970Driver() => _protocolName = GetProtocolName();

    public Task<PointResult?> ReadAsync(ProtocolEntity protocol, string devId, PointEntity point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
    {
        var protocol = new ProtocolEntity
        {
            IPAddress = writeTask.IPAddress,
            ProtocolPort = writeTask.ProtocolPort,
            ReceiveTimeOut = writeTask.ReceiveTimeOut,
            ConnectTimeOut = writeTask.ConnectTimeOut,
        };

        string address = writeTask.WriteDevice?.WritePoints?[0]?.Value ?? string.Empty;
        bool ok = false;
        string deviceMsg = string.Empty;

        try
        {
            using var client = new TcpClient();
            int connectTimeout = protocol.ConnectTimeOut > 0 ? protocol.ConnectTimeOut : 5000;
            var connectTask = client.ConnectAsync(protocol.IPAddress, protocol.ProtocolPort);
            if (await Task.WhenAny(connectTask, Task.Delay(connectTimeout, token)) != connectTask)
                throw new IOException("连接超时");

            client.NoDelay = true;
            client.ReceiveTimeout = 0;
            client.SendTimeout = connectTimeout;

            using var stream = client.GetStream();

            int finalTimeoutMs = protocol.ReceiveTimeOut > 0 ? protocol.ReceiveTimeOut : DefaultFinalTimeoutMs;
            int tailGraceMs = DefaultTailGraceMs;

            if (string.IsNullOrWhiteSpace(address))
            {
                // 地址为空，发送 FJ<stop>
                byte[] stopCmd = Encoding.ASCII.GetBytes("FJ<stop>");
                await WriteAllAsync(stream, stopCmd.AsMemory(0, stopCmd.Length), token);

                var reply = await ReplyStateMachineAsync(stream, finalTimeoutMs, tailGraceMs, token);
                ok = reply.Final && reply.Success;
                deviceMsg = reply.Message ?? (ok ? "OK" : "NO");
            }
            else
            {
                byte[] fileData = HexStringToBytes(address);
                if (fileData.Length == 0)
                    return false;

                int totalBytes = fileData.Length;
                int totalSeq = (totalBytes + DefaultChunkSize - 1) / DefaultChunkSize;

                var listenCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var listenTask = Task.Run(() => ReplyStateMachineAsync(stream, finalTimeoutMs, tailGraceMs, listenCts.Token), listenCts.Token);

                for (int seq = 0; seq < totalSeq; seq++)
                {
                    token.ThrowIfCancellationRequested();
                    int offset = seq * DefaultChunkSize;
                    int len = Math.Min(DefaultChunkSize, totalBytes - offset);
                    await WriteAllAsync(stream, fileData.AsMemory(offset, len), token);

                    if (PerChunkDelayMs > 0)
                        await Task.Delay(PerChunkDelayMs, token);

                    if (listenTask.IsCompletedSuccessfully && listenTask.Result.Final && !listenTask.Result.Success)
                        break;
                }

                var reply = await listenTask;
                ok = reply.Final && reply.Success;
                deviceMsg = reply.Message ?? (ok ? "OK" : "NO");
            }
        }
        catch (Exception ex) when (
            ex is ProtocolWhenConnFailedException ||
            ex is ProtocolIsNullWhenWriteException ||
            ex is NotSupportedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);
        }

        return ok;
    }

    private record ReplyResult(bool Final, bool Success, string? Message);

    private async Task<ReplyResult> ReplyStateMachineAsync(NetworkStream stream, int finalTimeoutMs, int tailGraceMs, CancellationToken ct)
    {
        var start = Environment.TickCount64;
        var buffer = new byte[ReadBufferSize];
        var acc = new StringBuilder();
        bool okSeen = false;
        bool noSeen = false;
        bool connectionClosed = false;

        while (!ct.IsCancellationRequested && (Environment.TickCount64 - start) < finalTimeoutMs)
        {
            try
            {
                var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(500);
                int r;
                try
                {
                    r = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), readCts.Token);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }

                if (r == 0)
                {
                    connectionClosed = true;
                    if (tailGraceMs > 0)
                    {
                        var graceStart = Environment.TickCount64;
                        while ((Environment.TickCount64 - graceStart) < tailGraceMs && !ct.IsCancellationRequested)
                        {
                            if (stream.DataAvailable)
                            {
                                int extra = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                                if (extra > 0)
                                {
                                    AppendAndParse(buffer, extra, acc, ref okSeen, ref noSeen);
                                    if (noSeen) return new ReplyResult(true, false, "NO");
                                    if (okSeen && !noSeen) return new ReplyResult(true, true, "OK");
                                }
                            }
                            await Task.Delay(50, ct);
                        }
                    }
                    break;
                }

                AppendAndParse(buffer, r, acc, ref okSeen, ref noSeen);

                if (noSeen) return new ReplyResult(true, false, "NO");
                if (okSeen) return new ReplyResult(true, true, "OK");
            }
            catch (IOException ioex)
            {
                return new ReplyResult(false, false, $"io_error:{ioex.Message}");
            }
            catch (Exception ex)
            {
                return new ReplyResult(false, false, $"error:{ex.Message}");
            }
        }

        if (noSeen) return new ReplyResult(true, false, "NO");
        if (okSeen) return new ReplyResult(true, true, "OK");
        if (connectionClosed) return new ReplyResult(false, false, acc.ToString().Trim());
        return new ReplyResult(false, false, acc.ToString().Trim());
    }

    private void AppendAndParse(byte[] buf, int len, StringBuilder acc, ref bool okSeen, ref bool noSeen)
    {
        var slice = Encoding.ASCII.GetString(buf, 0, len);
        acc.Append(slice);

        var text = acc.ToString();
        if (ContainsOk(text)) okSeen = true;
        if (ContainsNo(text)) noSeen = true;
    }

    private static bool ContainsOk(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.ToUpperInvariant();
        return s.Contains("OK");
    }
    private static bool ContainsNo(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.ToUpperInvariant();
        return s.Contains("NO");
    }

    private static async Task WriteAllAsync(NetworkStream stream, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        int sent = 0;
        while (sent < data.Length)
        {
            ct.ThrowIfCancellationRequested();
            var slice = data.Slice(sent);
            await stream.WriteAsync(slice, ct);
            sent += slice.Length;
        }
    }

    static byte[] HexStringToBytes(string hexString)
    {
        var charsToRemove = new[] { " ", "\r", "\n", "\t", ",", ";", "\r\n" };
        foreach (var c in charsToRemove)
        {
            hexString = hexString.Replace(c, "");
        }
        int len = hexString.Length;
        if (len % 2 != 0)
            throw new ArgumentException("十六进制字符串长度必须为偶数。");
        byte[] bytes = new byte[len / 2];
        for (int i = 0; i < len; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public string GetProtocolName() => "BottomImageF1970";
}

//using HslCommunication.Instrument.CJT;
//using HslCommunication.Instrument.DLT;
//using HslCommunication.ModBus;
//using KEDA_Common.CustomException;
//using KEDA_Common.Entity;
//using KEDA_Common.Enums;
//using KEDA_Common.Model;
//using KEDA_Controller.Base;
//using KEDA_Controller.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;
//using ZXing;

//namespace KEDA_Controller.Protocols.Special;
//[ProtocolType(KEDA_Common.Enums.ProtocolType.BottomImageF1970)]
//public class BottomImageF1970 : IProtocolDriver
//{
//    protected readonly string _protocolName;//协议名称
//    private Socket? _conn;

//    public BottomImageF1970() => _protocolName = GetProtocolName();

//    public Task<PointResult?> ReadAsync(ProtocolEntity protocol, PointEntity point, CancellationToken token)
//    {
//        throw new NotImplementedException();
//    }

//    #region 写方法
//    public virtual async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
//    {
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
//                await OnConnectionInitializedAsync(protocol, token);
//            }

//            if (writeTask.WriteDevice == null) return false;

//            var value = writeTask.WriteDevice.WritePoints[0].Value;

//            // 直接读取文件内容
//            byte[] fileData = HexStringToBytes(value);

//            if (fileData.Length == 0) return false;

//            // 发送前 8 字节（如果不足 8，则直接发送全部然后结束）
//            int headLen = Math.Min(8, fileData.Length);
//            await SendAllAsync(fileData.AsMemory(0, headLen), token);

//            int chunkSize = 4096; // 你可以保持原值或调整
//            int offset = headLen;

//            while (offset < fileData.Length)
//            {
//                token.ThrowIfCancellationRequested();

//                int size = Math.Min(chunkSize, fileData.Length - offset);
//                await SendAllAsync(fileData.AsMemory(offset, size), token);
//                offset += size;

//                // 每包等待 300ms
//                await Task.Delay(300, token);
//            }

//            byte[] buffer = new byte[1024];
//            int bytesRead = await _conn.ReceiveAsync(buffer);

//            string res = string.Empty;

//            if (bytesRead > 0)
//            {
//                res = Encoding.ASCII.GetString(buffer, 0, bytesRead);
//                int idx = res.IndexOf("\r\n", StringComparison.Ordinal);
//                if (idx >= 0)
//                    res = res.Substring(0, idx); // 只保留第一个换行符前的内容
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

//        return true;
//    }

//    // 发送全部数据的安全方法
//    private async Task SendAllAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
//    {
//        int sent = 0;
//        while (sent < data.Length)
//        {
//            ct.ThrowIfCancellationRequested();
//            if (_conn == null) return;
//            int n = await _conn.SendAsync(data.Slice(sent), SocketFlags.None, ct);
//            if (n <= 0)
//                throw new IOException("发送返回 0，连接可能已断开。");
//            sent += n;
//        }
//    }
//    #endregion

//    #region 读写公共方法，创建协议对象和连接协议
//    //子类实现：创建连接对象
//    private Socket CreateConnection(ProtocolEntity protocol, CancellationToken token)//一般不抛出异常
//    {
//        var conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
//        {
//            NoDelay = true,
//            SendBufferSize = 8192,
//            ReceiveBufferSize = 8192,
//            SendTimeout = protocol.ConnectTimeOut,
//            ReceiveTimeout = protocol.ReceiveTimeOut
//        };
//        return conn;
//    }

//    //子类可选实现：连接初始化后设置参数
//    protected virtual async Task OnConnectionInitializedAsync(ProtocolEntity protocol, CancellationToken token)
//    {
//        _conn?.Connect(protocol.IPAddress, protocol.ProtocolPort);//连接plc
//        await Task.CompletedTask;
//    }
//    #endregion

//    public virtual void Dispose()
//    {
//        _conn?.Dispose();
//        _conn = null;
//        GC.SuppressFinalize(this);
//    }

//    //获取协议名称
//    public string GetProtocolName() => "BottomImageF1970";

//    #region 翻译指令

//    static byte[] HexStringToBytes(string hexString)
//    {
//        // 去除空格、换行、制表符、逗号、分号等常见分隔符
//        var charsToRemove = new[] { " ", "\r", "\n", "\t", ",", ";", "\r\n" };
//        foreach (var c in charsToRemove)
//        {
//            hexString = hexString.Replace(c, "");
//        }
//        int len = hexString.Length;
//        if (len % 2 != 0)
//            throw new ArgumentException("十六进制字符串长度必须为偶数。");
//        byte[] bytes = new byte[len / 2];
//        for (int i = 0; i < len; i += 2)
//        {
//            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
//        }
//        return bytes;
//    }

//    #endregion
//}
