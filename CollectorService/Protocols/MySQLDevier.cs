using CollectorService.CustomException;
using CollectorService.Models;
using HslCommunication.Instrument.DLT;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Protocols;
public class MySQLDevier : IProtocolDriver
{
    private MySqlConnection? _conn;
    private string _protocolName = "MySql";
    public async Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        try
        {
            // 解析 Gateway 字段
            var gatewayParts = protocol.Gateway.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (gatewayParts.Length != 3)
                throw new InvalidOperationException("Gateway 字段格式错误，必须为 UserID,Password,Database");

            var userId = gatewayParts[0];
            var password = gatewayParts[1];
            var database = gatewayParts[2];

            // 组装连接字符串
            var connectionString = $"Server={protocol.IPAddress};Port={protocol.ProtocolPort};User ID={userId};Password={password};Database={database};Connection Timeout={protocol.ConnectTimeOut};";

            if (_conn == null)
            {
                _conn?.Dispose();
                _conn = new MySqlConnection(connectionString);
                _conn.Open();
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

            using var cmd = new MySqlCommand(point.Address, _conn);
            object? value = null;
            value = await cmd.ExecuteScalarAsync(token);

            if(value != null && value != DBNull.Value)
                result.Value = value;
            else
                throw new PointFailedException($"{_protocolName}协议读取采集点失败", new Exception());

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
            _conn.Dispose();
            _conn = null;
        }
        GC.SuppressFinalize(this);

    }
}
