using KEDA_CommonV2.CustomException;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Interfaces;
using MySqlConnector;
using System.Text.Json;

namespace KEDA_ControllerV2.Protocols.Sql;

[ProtocolType(ProtocolType.MySQL)]
public class MySqlDriver : IProtocolDriver
{
    private readonly string _protocolName = "MySql";

    public async Task<ProtocolResult?> ReadAsync(ProtocolDto protocol, CancellationToken token)
    {
        try
        {
            var dbProtocol = new DatabaseProtocolDto();
            using var conn = CreateConnection(protocol, out dbProtocol);

            var result = new ProtocolResult
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ProtocolId = protocol.Id,
                ProtocolType = protocol.ProtocolType.ToString(),
                EquipmentResults = [],
                StartTime = string.Empty,
                EndTime = string.Empty,
            };

            var protocolStartTime = DateTime.Now;
            result.StartTime = protocolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            await conn.OpenAsync(token);

            var dataDict = await QueryDataDictionaryAsync(conn, dbProtocol.QuerySqlString, token);

            foreach (var equipment in protocol.Equipments)
            {
                var equipmentResult = new EquipmentResult
                {
                    EquipmentId = equipment.Id,
                    EquipmentName = equipment.Name,
                    PointResults = []
                };

                var startTime = DateTime.Now;
                equipmentResult.StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss.fff");


                foreach (var point in equipment.Parameters)
                {
                    var pointResult = BuildPointResult(point, dataDict);
                    equipmentResult.PointResults.Add(pointResult);
                }

                var endTime = DateTime.Now;
                equipmentResult.EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                equipmentResult.ElapsedMs = (long)(endTime - startTime).TotalMilliseconds;

                equipmentResult.TotalPoints = equipmentResult.PointResults.Count;
                equipmentResult.SuccessPoints = equipmentResult.PointResults.Count(p => p.ReadIsSuccess);
                equipmentResult.FailedPoints = equipmentResult.PointResults.Count(p => !p.ReadIsSuccess);
                equipmentResult.ReadIsSuccess = equipmentResult.PointResults.All(p => p.ReadIsSuccess);
                equipmentResult.ErrorMsg = equipmentResult.PointResults.FirstOrDefault(p => !p.ReadIsSuccess)?.ErrorMsg ?? string.Empty;

                result.EquipmentResults.Add(equipmentResult);
            }

            var protocolEndTime = DateTime.Now;
            result.EndTime = protocolEndTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            result.ElapsedMs = (long)(protocolEndTime - protocolStartTime).TotalMilliseconds;

            result.TotalEquipments = result.EquipmentResults.Count;
            result.SuccessEquipments = result.EquipmentResults.Count(d => d.ReadIsSuccess);
            result.FailedEquipments = result.EquipmentResults.Count(d => !d.ReadIsSuccess);

            result.TotalPoints = result.EquipmentResults.Sum(d => d.TotalPoints);
            result.SuccessPoints = result.EquipmentResults.Sum(d => d.SuccessPoints);
            result.FailedPoints = result.EquipmentResults.Sum(d => d.FailedPoints);

            result.ReadIsSuccess = result.EquipmentResults.All(d => d.ReadIsSuccess);
            result.ErrorMsg = result.EquipmentResults.FirstOrDefault(d => !d.ReadIsSuccess)?.ErrorMsg ?? string.Empty;

            return result;
        }
        catch (MySqlException ex)
        {
            var result = BuildFailedProtocolResult(protocol, $"MySQL请求失败: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            throw new ProtocolDefaultException($"{_protocolName}协议操作失败", ex);
        }
    }

    private MySqlConnection CreateConnection(ProtocolDto protocol, out DatabaseProtocolDto dbProtocol)
    {
        if (protocol is DatabaseProtocolDto p)
        {
            var connStr = GetConnectionString(p);
            var conn = new MySqlConnection(connStr);
            dbProtocol = p;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 ApiProtocol，无法进行操作。");
    }

    private static string GetConnectionString(DatabaseProtocolDto dbProtocol)
    {
        if (!string.IsNullOrWhiteSpace(dbProtocol.DatabaseConnectString))
        {
            return dbProtocol.DatabaseConnectString;
        }

        // 拼接标准MySQL连接字符串
        var builder = new MySqlConnectionStringBuilder
        {
            Server = dbProtocol.IpAddress,
            Port = (uint)(dbProtocol.ProtocolPort > 0 ? dbProtocol.ProtocolPort : 3306),
            Database = dbProtocol.DatabaseName,
            UserID = dbProtocol.Account,
            Password = dbProtocol.Password
        };

        if (!string.IsNullOrWhiteSpace(dbProtocol.AdditionalOptions))
        {
            builder.ConnectionString += ";" + dbProtocol.AdditionalOptions;
        }

        return builder.ConnectionString;
    }

    private static ProtocolResult BuildFailedProtocolResult(ProtocolDto protocol, string errorMsg)
    {
        var result = new ProtocolResult
        {
            Id = Guid.NewGuid().ToString("N"),
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            ProtocolId = protocol.Id,
            ProtocolType = protocol.ProtocolType.ToString(),
            EquipmentResults = [],
            ReadIsSuccess = false,
            ErrorMsg = errorMsg,
            StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            ElapsedMs = 0
        };

        if (protocol.Equipments != null)
        {
            foreach (var equipment in protocol.Equipments)
            {
                var equipmentResult = new EquipmentResult
                {
                    EquipmentId = equipment.Id,
                    EquipmentName = equipment.Name,
                    ReadIsSuccess = false,
                    ErrorMsg = errorMsg,
                    PointResults = [],
                    TotalPoints = 0,
                    SuccessPoints = 0,
                    FailedPoints = 0,
                    StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ElapsedMs = 0
                };

                if (equipment.Parameters != null)
                {
                    foreach (var point in equipment.Parameters)
                    {
                        equipmentResult.PointResults.Add(new PointResult
                        {
                            Address = point.Label,
                            Label = point.Label,
                            DataType = point.DataType,
                            ReadIsSuccess = false,
                            Value = null,
                            ErrorMsg = errorMsg,
                            ElapsedMs = 0
                        });
                    }
                }

                equipmentResult.TotalPoints = equipmentResult.PointResults.Count;
                equipmentResult.SuccessPoints = equipmentResult.PointResults.Count(p => p.ReadIsSuccess);
                equipmentResult.FailedPoints = equipmentResult.TotalPoints - equipmentResult.SuccessPoints;

                result.EquipmentResults.Add(equipmentResult);
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, object>> QueryDataDictionaryAsync(MySqlConnection conn, string sql, CancellationToken token)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new MySqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            // 取第一列为key，第二列为value
            var key = reader.GetValue(0)?.ToString() ?? string.Empty;
            var value = reader.GetValue(1);
            dict[key] = value;
        }
        return dict;
    }

    private static PointResult BuildPointResult(ParameterDto point, Dictionary<string, object> dataDict)
    {
        var label = point.Label;
        var address = point.Address;
        object? value = null;
        bool found = false;

        if (!string.IsNullOrEmpty(label) && dataDict.TryGetValue(label, out value))
            found = true;
        else if (!string.IsNullOrEmpty(address) && dataDict.TryGetValue(address, out value))
            found = true;

        return new PointResult
        {
            DataType = point.DataType,
            Label = label,
            Address = address,
            Value = value,
            ReadIsSuccess = found,
            ErrorMsg = found ? string.Empty : "未找到对应数据"
        };
    }

    public string GetProtocolName() => _protocolName;

    #region 不用实现
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public Task<PointResult?> ReadAsync(ProtocolDto protocol, string equipmentId, ParameterDto point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> WriteAsync(WriteTask writeTask, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    #endregion
}
