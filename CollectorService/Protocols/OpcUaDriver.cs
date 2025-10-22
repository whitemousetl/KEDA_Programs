using CollectorService.CustomException;
using CollectorService.Models;
using HslCommunication.Instrument.DLT;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using Opc.Ua;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CollectorService.Protocols;
public class OpcUaDriver : IProtocolDriver
{
    private OpcUaClient? _conn;
    private string _protocolName = "OpcUaClient";

    public async Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        try
        {
            if (_conn == null)
            {
                var ip = protocol.IPAddress;
                var port = int.Parse(protocol.ProtocolPort);
                _conn = new();

                // 账号密码从Gateway字段获取
                var gatewayParts = protocol.Remark?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var username = gatewayParts != null && gatewayParts.Length > 0 ? gatewayParts[0] : "";
                var password = gatewayParts != null && gatewayParts.Length > 1 ? gatewayParts[1] : "";

                _conn = new ();
                //测试用
                username = "admin62";
                password = "drewxjks782";
                // 设置账号密码
                _conn.UserIdentity = new UserIdentity(username, password);

                await _conn.ConnectServer($"opc.tcp://{ip}:{port}");
            }

        }
        catch (Exception ex)
        {
            if (ex is ProtocolFailedException)
                throw;
            throw new ProtocolException($"{_protocolName}协议连接失败", ex);
        }

        try
        {
            var dataType = Enum.Parse<DataType>(point.DataType);

            var result = new PointCollectTask
            {
                Protocol = protocol,
                Device = device,
                Point = point,
                DataType = dataType
            };

            //测试用
            var res1 = _conn.ReadNode<short>("ns=2;s=Tag.应急.Real除尘器应急温度");
            Console.WriteLine($"ns=2;s=Tag.应急.Real除尘器应急温度d值是{res1}");

            //switch (dataType)
            //{
            //    case DataType.Bool:
            //        {
            //            var res = await _conn.ReadNodeAsync<bool>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.UShort:
            //        {
            //            var res = await _conn.ReadNodeAsync<ushort>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.Short:
            //        {
            //            var res = await _conn.ReadNodeAsync<short>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.UInt:
            //        {
            //            var res = await _conn.ReadNodeAsync<uint>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.Int:
            //        {
            //            var res = await _conn.ReadNodeAsync<int>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.Float:
            //        {
            //            var res = await _conn.ReadNodeAsync<float>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.Double:
            //        {
            //            var res = await _conn.ReadNodeAsync<double>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    case DataType.String:
            //        {
            //            var length = ushort.Parse(point.Length);
            //            var res = await _conn.ReadNodeAsync<string>(point.Address);
            //            result.Value = res;
            //            break;
            //        }
            //    default:
            //        break;
            //}

            return result;
        }
        catch (Exception ex)
        {
            if (ex is PointFailedException)
                throw;
            throw new PointException($"{_protocolName}协议读取采集点失败", ex);
        }

    }

    public void Dispose()
    {
        if (_conn != null)
        {
            _conn.Disconnect();
            _conn = null;
        }
        GC.SuppressFinalize(this);
    }
}
