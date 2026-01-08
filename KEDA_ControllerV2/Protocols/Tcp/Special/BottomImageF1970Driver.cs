using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace KEDA_ControllerV2.Protocols.Tcp.Special;

[SupportedProtocolType(KEDA_CommonV2.Enums.ProtocolType.BottomImageF1970)]
public class BottomImageF1970Driver : IProtocolDriver
{
    protected readonly string _protocolName;
    private const int DefaultChunkSize = 2048;
    private const int ReadBufferSize = 256;
    private const int DefaultFinalTimeoutMs = 30000;
    private const int DefaultTailGraceMs = 2000;
    private const int PerChunkDelayMs = 50;

    public BottomImageF1970Driver() => _protocolName = GetProtocolName();

    public Task<PointResult?> ReadAsync(ProtocolDto protocol, string equipmentId, ParameterDto point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token)
    {
        if (writeTask.Protocol is LanProtocolDto lanProtocol)
        {
            bool ok = false;
            try
            {
                string address = writeTask?.Protocol.Equipments[0].Parameters[0].Value ?? string.Empty;
                string equipmentMsg = string.Empty;

                using var client = new TcpClient();
                int connectTimeout = lanProtocol.ConnectTimeOut > 0 ? lanProtocol.ConnectTimeOut : 5000;
                var connectTask = client.ConnectAsync(lanProtocol.IpAddress, lanProtocol.ProtocolPort);
                if (await Task.WhenAny(connectTask, Task.Delay(connectTimeout, token)) != connectTask)
                    throw new IOException("连接超时");

                client.NoDelay = true;
                client.ReceiveTimeout = 0;
                client.SendTimeout = connectTimeout;

                using var stream = client.GetStream();

                int finalTimeoutMs = lanProtocol.ReceiveTimeOut > 0 ? lanProtocol.ReceiveTimeOut : DefaultFinalTimeoutMs;
                int tailGraceMs = DefaultTailGraceMs;

                if (string.IsNullOrWhiteSpace(address))
                {
                    // 地址为空，发送 FJ<stop>
                    byte[] stopCmd = Encoding.ASCII.GetBytes("FJ<stop>");
                    await WriteAllAsync(stream, stopCmd.AsMemory(0, stopCmd.Length), token);

                    var reply = await ReplyStateMachineAsync(stream, finalTimeoutMs, tailGraceMs, token);
                    ok = reply.Final && reply.Success;
                    equipmentMsg = reply.Message ?? (ok ? "OK" : "NO");
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
                    equipmentMsg = reply.Message ?? (ok ? "OK" : "NO");
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
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
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

        while (!ct.IsCancellationRequested && Environment.TickCount64 - start < finalTimeoutMs)
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
                        while (Environment.TickCount64 - graceStart < tailGraceMs && !ct.IsCancellationRequested)
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

    private static byte[] HexStringToBytes(string hexString)
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

    public Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}