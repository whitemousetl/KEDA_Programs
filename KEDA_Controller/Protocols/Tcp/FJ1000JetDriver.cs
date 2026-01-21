using HslCommunication.Profinet.Freedom;
using KEDA_Common.CustomException;
using KEDA_Common.Entity;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using KEDA_Controller.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Tcp;
[ProtocolType(ProtocolType.FJ1000Jet)]
public class FJ1000JetDriver : HslTcpBaseProtocolDriver<FreedomTcpNet>//方嘉砖侧码，喷墨打印机
{
    public FJ1000JetDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override FreedomTcpNet CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }

    #region 写方法
    public override async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
    {
        //初始化_conn
        var protocol = new ProtocolEntity
        {
            IPAddress = writeTask.IPAddress,
            ProtocolPort = writeTask.ProtocolPort,
            ReceiveTimeOut = writeTask.ReceiveTimeOut,
            ConnectTimeOut = writeTask.ConnectTimeOut,
        };

        try
        {
            if (_conn == null)
            {
                _conn = CreateConnection(protocol, token);
                await OnConnectionInitializedAsync(token);
            }

            if (writeTask.WriteDevice == null) return false;

            var hexList = new ConcurrentBag<byte>();
            hexList.Add(0x1B);
            hexList.Add(0x02);
            var station = StringToHex(writeTask.WriteDevice.WritePoints[0].StationNo);
            //地址
            hexList.Add(station);
            hexList.Add(0x1D);
            var points = writeTask.WriteDevice.WritePoints;
            var strCount = (byte)points.Length;
            //字段总数
            hexList.Add(strCount);

            //文本信息
            var msgByteList = ConvertInformationIntoHexadecimal(points);
            foreach (var b in msgByteList)
                hexList.Add(b);
            hexList.Add(0x1B);
            hexList.Add(0x03);
            //校验码
            var checksum = Checksum([.. hexList]);
            hexList.Add(checksum);

            await _conn.ReadFromCoreServerAsync([.. hexList]);
        }
        catch (Exception ex) when (
        ex is ProtocolWhenConnFailedException ||//连接plc失败异常
        ex is ProtocolIsNullWhenWriteException ||//当写入时协议为空异常
        ex is NotSupportedException) //不支持的类型异常
        {
            // 直接抛出已知的自定义异常
            throw;
        }
        catch (Exception ex)
        {
            // 统一处理未知异常
            throw new ProtocolDefaultException($"FJ1000Jet协议操作失败", ex);//抛出默认异常
        }

        return true;
    }
    #endregion

    #region 翻译指令
    private byte[] ConvertInformationIntoHexadecimal(WritePoint[] points)
    {
        var msgByteList = new ConcurrentBag<byte>();
        foreach (var point in points)
        {
            //字段标识
            msgByteList.Add(StringToHex(point.Label));
            ////文本字节数
            //msgByteList.Add((byte)point.Address.Length);
            ////文本
            //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(point.Address);
            //msgByteList.AddRange(bytes);
            byte[] bytes = Encoding.UTF8.GetBytes(point.Value);
            msgByteList.Add((byte)bytes.Length); // 用字节长度
            foreach (var b in bytes)
                msgByteList.Add(b);

        }

        return msgByteList.ToArray();
    }

    public byte Checksum(byte[] bytes)
    {
        // 计算所有字节的总和
        int sum = 0;
        foreach (byte b in bytes)
        {
            sum += b;
        }

        // 对256求模
        int mod = sum % 256;

        // 计算2补码
        int checksum = ~mod + 1 & 0xFF;

        return (byte)checksum;
    }

    private byte StringToHex(string station)
    {
        if (!int.TryParse(station, out var result))
        {
            var msg = "输入不是有效的十进制数字字符串";
            throw new ArgumentException(msg);
        }

        if (result < 0 || result > 255)
        {
            var msg = "数字必须在0到255之间";
            throw new ArgumentOutOfRangeException(msg);
        }

        return (byte)result;
    }
    #endregion
}
