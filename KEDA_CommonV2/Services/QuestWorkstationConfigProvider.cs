using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Converters;
using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace KEDA_CommonV2.Services;

public class QuestWorkstationConfigProvider : IWorkstationConfigProvider
{
    private readonly ILogger<QuestWorkstationConfigProvider> _logger;
    private readonly string _connectionString;
    private readonly string _configTableName;

    public QuestWorkstationConfigProvider(ILogger<QuestWorkstationConfigProvider> logger)
    {
        _logger = logger;
        _connectionString = SharedConfigHelper.DatabaseSettings.QuestDb;
        _configTableName = SharedConfigHelper.DatabaseSettings.ConfigTableName;
    }

    public async Task<WorkstationConfig?> GetLatestWorkstationConfigEntityAsync(CancellationToken token)
    {
        string sql = $@"
            SELECT ConfigJson, SaveTimeLocal
            FROM {_configTableName}
            ORDER BY SaveTime DESC
            LIMIT 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(token);
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync(token);
        if (await reader.ReadAsync(token))
        {
            var configJson = reader.GetString(0);
            var saveTimeLocal = reader.GetString(1);
            return new WorkstationConfig
            {
                ConfigJson = configJson,
                SaveTimeLocal = saveTimeLocal
            };
        }
        return null;
    }

    public async Task<Workstation?> GetLatestWrokstationAsync(CancellationToken token)
    {
        var configEntity = await GetLatestWorkstationConfigEntityAsync(token);
        if (configEntity == null || string.IsNullOrWhiteSpace(configEntity.ConfigJson))
            return null;

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ProtocolJsonConverter());
        return JsonSerializer.Deserialize<Workstation>(configEntity.ConfigJson, options);
    }

    public async Task<Protocol?> GetProtocolByProtocolIdAsync(string protocolId, CancellationToken token)
    {
        try
        {
            var workstation = await GetLatestWrokstationAsync(token);
            if (workstation?.Protocols == null)
                return null;
            return workstation.Protocols.FirstOrDefault(p => p.ProtocolId == protocolId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据协议Id查找协议时发生异常: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<Protocol?> GetProtocolByDeviceIdAsync(string deviceId, CancellationToken token)
    {
        try
        {
            var workstation = await GetLatestWrokstationAsync(token);
            if (workstation?.Protocols == null)
                return null;
            return workstation.Protocols.FirstOrDefault(p => p.Devices.Any(d => d.EquipmentId == deviceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据设备Id查找协议时发生异常: {Message}", ex.Message);
            return null;
        }
    }

    public bool IsConfigChanged(WorkstationConfig? latestConfig, string lastSaveTimeLocal)
    {
        if (latestConfig == null)
            return false;
        return latestConfig.SaveTimeLocal != lastSaveTimeLocal;
    }

    public async Task SaveConfigAsync(WorkstationConfig entity, CancellationToken token)
    {
        // 校验必填字段
        if (string.IsNullOrWhiteSpace(entity.ConfigJson))
            throw new ArgumentException("ConfigJson 不能为空", nameof(entity.ConfigJson));
        if (entity.SaveTime == default)
            throw new ArgumentException("SaveTime 不能为空且必须为有效的UTC时间", nameof(entity.SaveTime));
        if (string.IsNullOrWhiteSpace(entity.SaveTimeLocal))
            throw new ArgumentException("SaveTimeLocal 不能为空", nameof(entity.SaveTimeLocal));

        // 构造字段和值
        var columns = new List<string> { "ConfigJson", "SaveTime", "SaveTimeLocal" };
        var values = new List<string>
        {
            $"'{entity.ConfigJson.Replace("'", "''")}'",
            $"'{entity.SaveTime:yyyy-MM-ddTHH:mm:ss.fffZ}'",
            $"'{entity.SaveTimeLocal.Replace("'", "''")}'"
        };

        var insertSql = $@"
        INSERT INTO {_configTableName} ({string.Join(", ", columns)})
        VALUES ({string.Join(", ", values)})";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(token);
        await using var cmd = new NpgsqlCommand(insertSql, conn);

        try
        {
            await cmd.ExecuteNonQueryAsync(token);
            _logger.LogInformation("工作站配置已成功保存到数据库，保存时间: {SaveTime}, 本地时间: {SaveTimeLocal}", entity.SaveTime, entity.SaveTimeLocal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存工作站配置到数据库时发生异常: {Message}", ex.Message);
            throw;
        }
    }
}