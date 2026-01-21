using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Entity;
using KEDA_CommonV2.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KEDA_CommonV2.Services;

public class WriteTaskLogService : IWriteTaskLogService
{
    private readonly ILogger<WriteTaskLogService> _logger;
    private readonly string _connectionString;
    private readonly ISharedConfigHelper _sharedConfigHelper;

    public WriteTaskLogService(ILogger<WriteTaskLogService> logger, ISharedConfigHelper sharedConfigHelper)
    {
        _logger = logger;
        _sharedConfigHelper = sharedConfigHelper;
        _connectionString = _sharedConfigHelper.DatabaseSettings.QuestDb;
    }

    public async Task AddLogAsync(WriteTaskLog log)
    {
        // 校验必填字段
        if (string.IsNullOrWhiteSpace(log.UUID))
            throw new ArgumentException("UUID 不能为空", nameof(log.UUID));
        if (log.Time == default)
            throw new ArgumentException("Time 不能为空且必须为有效的UTC时间", nameof(log.Time));
        if (string.IsNullOrWhiteSpace(log.WriteTaskJson))
            throw new ArgumentException("WriteTaskJson 不能为空", nameof(log.WriteTaskJson));
        if (string.IsNullOrWhiteSpace(log.TimeLocal))
            throw new ArgumentException("TimeLocal 不能为空", nameof(log.TimeLocal));
        if (string.IsNullOrWhiteSpace(log.Msg))
            log.Msg = string.Empty;

        // 构造字段和值
        var columns = new List<string> { "UUID", "EquipmentType", "WriteTaskJson", "Time", "TimeLocal", "IsSuccess", "Msg" };
        var values = new List<string>
        {
            $"'{log.UUID.Replace("'", "''")}'", // string
            $"{(int)log.EquipmentType}",           // int (枚举)
            $"'{log.WriteTaskJson.Replace("'", "''")}'", // string
            $"'{log.Time:yyyy-MM-ddTHH:mm:ss.fffZ}'",    // DateTime
            $"'{log.TimeLocal.Replace("'", "''")}'",     // string
            log.IsSuccess ? "true" : "false",            // bool
            $"'{log.Msg.Replace("'", "''")}'"            // string
        };

        var insertSql = $@"
        INSERT INTO WriteTaskLog ({string.Join(", ", columns)})
        VALUES ({string.Join(", ", values)})";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(insertSql, conn);

        try
        {
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("写任务日志已成功插入，UUID: {UUID}, 时间: {Time}", log.UUID, log.Time);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写任务日志插入失败: {Message}", ex.Message);
        }
    }
}