using CollectorService.CustomException;
using CollectorService.Models;
using HslCommunication.Core;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Omron;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Protocols;
public class ModbusProtocolDriver : IProtocolDriver
{
    private ModbusTcpNet? _conn;
    private string _protocolName = "ModbusTcp";
    public async Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        try
        {
            if (_conn == null)
            {
                var ip = protocol.IPAddress;
                var port = int.Parse(protocol.ProtocolPort);
                _conn = new (ip, port);
                var connRes = await _conn.ConnectServerAsync();
                if (!connRes.IsSuccess)
                    throw new ProtocolFailedException($"{_protocolName}协议连接失败: {connRes.Message}", new Exception(connRes.Message));
                else
                    Console.WriteLine("*************************连接成功！！！！***********************");
                _conn.ReceiveTimeOut = int.Parse(protocol.ReceiveTimeOut);
                _conn.ConnectTimeOut = int.Parse(protocol.ConnectTimeOut);
                _conn.AddressStartWithZero = bool.Parse(protocol.AddressStartWithZero);
                _conn.DataFormat = Enum.Parse<DataFormat>(protocol.Format);
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

            var station = byte.Parse(device.StationNo);
            _conn.Station = station;

            var result = new PointCollectTask
            {
                Protocol = protocol,
                Device = device,
                Point = point,
                DataType = dataType
            };

            if (!string.IsNullOrEmpty(point.Desc))
                _conn.DataFormat = Enum.Parse<DataFormat>(point.Desc);
            else
                _conn.DataFormat = Enum.Parse<DataFormat>(protocol.Format);

            switch (dataType)
            {
                case DataType.Bool:
                    {
                        var res = await _conn.ReadBoolAsync(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.UShort:
                    {
                        var res = await _conn.ReadUInt16Async(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.Short:
                    {
                        var res = await _conn.ReadInt16Async(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.UInt:
                    {
                        var res = await _conn.ReadUInt32Async(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.Int:
                    {
                        var res = await _conn.ReadInt32Async(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.Float:
                    {
                        var res = await _conn.ReadFloatAsync(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.Double:
                    {
                        var res = await _conn.ReadDoubleAsync(point.Address);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                case DataType.String:
                    {
                        var length = ushort.Parse(point.Length);
                        var res = await _conn.ReadStringAsync(point.Address, length);
                        if (!res.IsSuccess)
                            throw new PointFailedException($"{_protocolName}协议读取采集点失败: {res.Message}", new Exception(res.Message));
                        result.Value = res.Content;
                        break;
                    }
                default:
                    break;
            }

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
            _conn.ConnectClose();
            _conn = null;
        }
        GC.SuppressFinalize(this);
    }
}
