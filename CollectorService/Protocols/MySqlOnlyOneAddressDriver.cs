using CollectorService.CustomException;
using CollectorService.Models;
using KEDA_Share.Entity;
using KEDA_Share.Enums;
using MySqlConnector;

namespace CollectorService.Protocols;
public class MySqlOnlyOneAddressDriver : IProtocolDriver
{
    private MySqlConnection? _conn;
    private string _protocolName = "MySqlOnlyOneAddress";
    public Task<PointCollectTask?> ReadAsync(Protocol protocol, Device device, Point point, CancellationToken token)
    {
        throw new NotImplementedException();
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

    public async Task<DeviceResult> ReadAsync(Protocol protocol, Device device,  CancellationToken token)
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


        var deviceResult = new DeviceResult();

        try
        {
            using var cmd = new MySqlCommand(device.Points[0].Address, _conn);
            using var reader = await cmd.ExecuteReaderAsync(token);

            deviceResult.DevId = device.EquipmentID;
            deviceResult.PointResults = [];

            while (reader.Read())
            {
                object? name = reader.GetValue(0);
                object? value = reader.GetValue(1);

                DataType type;

                if (value is string)
                    type = DataType.String;
                else if (value is int)
                    type = DataType.Int;
                else if (value is float)
                    type = DataType.Float;
                else if (value is double)
                    type = DataType.Double;
                else
                    type = DataType.String;

                var result = new PointResult()
                {
                    Label = name.ToString(),
                    Result = value,
                    DataType = type
                };

                deviceResult.PointResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            if (ex is PointFailedException)
                throw;
            throw new PointException($"{_protocolName}协议读取采集点失败", ex);
        }

        return deviceResult;
       
    }
}
