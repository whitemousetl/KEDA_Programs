using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;
using System.Text.Json;

namespace KEDA_CommonV2.Services;

public class QuestDbDeviceDataStorageService : IDeviceDataStorageService
{
    private readonly ILogger<QuestDbDeviceDataStorageService> _logger;
    private readonly string _connectionString;
    private readonly DeviceDataStorageSettings _storageOptions;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tableColumnsCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tableLocks = new();

    public QuestDbDeviceDataStorageService(
        IOptions<DeviceDataStorageSettings> storageOptions,
        ILogger<QuestDbDeviceDataStorageService> logger)
    {
        _logger = logger;
        _storageOptions = storageOptions.Value;
        _connectionString = SharedConfigHelper.DatabaseSettings.QuestDb;
    }

    /// <summary>
    /// 保存设备数据到 QuestDB
    /// </summary>
    public async Task SaveDeviceDataAsync(string deviceId, string jsonData, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(jsonData))
        {
            _logger.LogWarning("设备ID或数据为空，跳过存储");
            return;
        }

        try
        {
            var dataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData);
            if (dataDict == null || dataDict.Count == 0)
            {
                _logger.LogWarning("设备 {DeviceId} 的数据解析后为空", deviceId);
                return;
            }

            dataDict.Remove("DeviceId");
            var tableName = SanitizeTableName(deviceId);

            if (string.IsNullOrEmpty(tableName)) return;

            var tableLock = _tableLocks.GetOrAdd(tableName, _ => new SemaphoreSlim(1, 1));
            await tableLock.WaitAsync(token);

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(token);

                // 确保表存在
                await EnsureTableExistsAsync(conn, tableName, dataDict, token);

                // 插入数据
                await InsertDataAsync(conn, tableName, dataDict, token);

                _logger.LogDebug("成功保存设备 {DeviceId} 的数据到 QuestDB 表 {TableName}", deviceId, tableName);
            }
            finally
            {
                tableLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存设备 {DeviceId} 数据到 QuestDB 时发生异常", deviceId);
        }
    }

    /// <summary>
    /// 确保表存在（QuestDB 自动添加列）
    /// </summary>
    private async Task EnsureTableExistsAsync(
        NpgsqlConnection conn,
        string tableName,
        Dictionary<string, JsonElement> dataDict,
        CancellationToken token)
    {
        // 检查表是否存在
        var checkTableSql = $@"
            SELECT COUNT(*)
            FROM tables()
            WHERE table_name = '{tableName}'";

        await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
        var tableCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(token));

        if (tableCount == 0)
            await CreateTableAsync(conn, tableName, dataDict, token);
        else
            await EnsureColumnsExistAsync(conn, tableName, dataDict, token);
    }

    /// <summary>
    /// 创建 QuestDB 表（带 TTL 自动清理）
    /// </summary>
    private async Task CreateTableAsync(
        NpgsqlConnection conn,
        string tableName,
        Dictionary<string, JsonElement> dataDict,
        CancellationToken token)
    {
        var columnDefinitions = new List<string>
        {
            "utc_timestamp TIMESTAMP",  // UTC时间，designated timestamp
            "time STRING",              // 原始time字符串
            "timestamp LONG"            // 原始时间戳
        };

        // 其他字段全部用 STRING 类型
        foreach (var kvp in dataDict)
        {
            if (kvp.Key.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
                continue;

            var columnName = SanitizeColumnName(kvp.Key);
            columnDefinitions.Add($"{columnName} STRING");
        }

        // ✅ 添加 TTL 配置（根据配置文件中的保留天数）
        var createTableSql = $@"
        CREATE TABLE {tableName} (
            {string.Join(", ", columnDefinitions)}
        ) TIMESTAMP(utc_timestamp) PARTITION BY DAY";

        await using var cmd = new NpgsqlCommand(createTableSql, conn);
        await cmd.ExecuteNonQueryAsync(token);

        // 之后单独设置 TTL（如果配置了保留天数）
        var ttlDays = _storageOptions.DataRetentionDays;
        if (ttlDays > 0)
        {
            var alterTtlSql = $"ALTER TABLE {tableName} SET TTL {ttlDays}d";
            await using var ttlCmd = new NpgsqlCommand(alterTtlSql, conn);
            await ttlCmd.ExecuteNonQueryAsync(token);
        }

        _logger.LogInformation("成功创建 QuestDB 表 {TableName}，包含 {ColumnCount} 个字段",
            tableName, columnDefinitions.Count);

        _tableColumnsCache[tableName] = new HashSet<string>(
            dataDict.Keys.Where(k => !k.Equals("timestamp", StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 确保列存在（QuestDB 动态添加列）
    /// </summary>
    private async Task EnsureColumnsExistAsync(
        NpgsqlConnection conn,
        string tableName,
        Dictionary<string, JsonElement> dataDict,
        CancellationToken token)
    {
        // 获取现有列
        if (!_tableColumnsCache.TryGetValue(tableName, out var existingColumns))
        {
            var getColumnsSql = $@"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_name = '{tableName}'";

            await using var cmd = new NpgsqlCommand(getColumnsSql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(token);

            existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(token))
            {
                existingColumns.Add(reader.GetString(0));
            }
            _tableColumnsCache[tableName] = existingColumns;
        }

        // 添加新列
        var newColumns = new List<(string Name, string Type)>();
        // QuestDB 动态添加列
        foreach (var kvp in dataDict)
        {
            if (kvp.Key.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
                continue;

            var columnName = SanitizeColumnName(kvp.Key);
            if (!existingColumns.Contains(columnName))
            {
                // 强制所有新列为 STRING
                var columnType = "STRING";
                try
                {
                    var alterSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
                    await using var cmd = new NpgsqlCommand(alterSql, conn);
                    await cmd.ExecuteNonQueryAsync(token);
                    existingColumns.Add(columnName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "添加列 {ColumnName} 到表 {TableName} 失败", columnName, tableName);
                }
            }
        }

        // QuestDB 动态添加列
        foreach (var (name, type) in newColumns)
        {
            try
            {
                var alterSql = $"ALTER TABLE {tableName} ADD COLUMN {name} {type}";
                await using var cmd = new NpgsqlCommand(alterSql, conn);
                await cmd.ExecuteNonQueryAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加列 {ColumnName} 到表 {TableName} 失败", name, tableName);
            }
        }

        if (newColumns.Any())
        {
            _logger.LogInformation("表 {TableName} 添加了 {Count} 个新列:  {Columns}",
                tableName, newColumns.Count, string.Join(", ", newColumns.Select(c => c.Name)));
        }
    }

    /// <summary>
    /// 确保所有表的 TTL 已更新（程序启动时仅执行一次）
    /// </summary>
    public async Task EnsureAllTablesTtlUpdatedAsync()
    {
        _logger.LogInformation("开始检查并更新所有设备表的 TTL 配置...");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 查询所有非系统表（排除 sys.  开头的表）
        var getTablesSql = @"
                SELECT table_name
                FROM tables()
                WHERE table_name NOT LIKE 'sys.%'
                  AND table_name NOT LIKE 'telemetry%'
                  AND partitionBy = 'DAY'";  // 只更新按天分区的表

        var tableNames = new List<string>();
        await using (var cmd = new NpgsqlCommand(getTablesSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        _logger.LogInformation("找到 {Count} 个设备表需要检查 TTL", tableNames.Count);

        // 批量更新 TTL
        int updatedCount = 0;
        int errorCount = 0;
        foreach (var tableName in tableNames)
        {
            try
            {
                await UpdateTableTtlAsync(conn, tableName);
                updatedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogWarning(ex, "更新表 {TableName} 的 TTL 失败", tableName);
            }
        }

        _logger.LogInformation(
            "TTL 配置更新完成。成功:  {Updated}, 失败: {Error}, 总计: {Total}",
            updatedCount, errorCount, tableNames.Count);
    }

    /// <summary>
    /// 更新单个表的 TTL
    /// </summary>
    private async Task UpdateTableTtlAsync(
        NpgsqlConnection conn,
        string tableName)
    {
        var ttlDays = _storageOptions.DataRetentionDays;

        var alterTtlSql = $"ALTER TABLE {tableName} SET TTL {ttlDays}d";

        await using var cmd = new NpgsqlCommand(alterTtlSql, conn);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogDebug("已更新表 {TableName} 的 TTL 为 {TtlDays} 天", tableName, ttlDays);
    }

    //private static async Task InsertDataAsync(
    //NpgsqlConnection conn,
    //string tableName,
    //Dictionary<string, JsonElement> dataDict,
    //CancellationToken token)
    //{
    //    var columns = new List<string>();
    //    var values = new List<string>();

    //    // 添加 utc_timestamp 字段
    //    columns.Add("utc_timestamp");
    //    values.Add($"'{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}'");

    //    // 处理 time 字段（字符串）
    //    if (dataDict.TryGetValue("time", out var timeElement))
    //    {
    //        columns.Add("time");
    //        values.Add($"'{timeElement.GetString()?.Replace("'", "''")}'");
    //        dataDict.Remove("time");
    //    }

    //    // 处理 timestamp 字段
    //    if (dataDict.TryGetValue("timestamp", out var timestampElement))
    //    {
    //        columns.Add("timestamp");
    //        values.Add(ConvertToSqlValue(timestampElement));
    //        dataDict.Remove("timestamp");
    //    }

    //    // 其他字段
    //    foreach (var kvp in dataDict)
    //    {
    //        var columnName = SanitizeColumnName(kvp.Key);
    //        values.Add(ConvertToSqlValue(kvp.Value));
    //        columns.Add(columnName);
    //    }

    //    var insertSql = $@"
    //    INSERT INTO {tableName} ({string.Join(", ", columns)})
    //    VALUES ({string.Join(", ", values)})";

    //    await using var cmd = new NpgsqlCommand(insertSql, conn);
    //    await cmd.ExecuteNonQueryAsync(token);



    //// 处理 timestamp
    //if (dataDict.TryGetValue("timestamp", out var timestampElement))
    //{
    //    // 转换 timestamp 为 UTC 时间
    //    long ts = timestampElement.ValueKind == JsonValueKind.Number
    //        ? timestampElement.GetInt64()
    //        : long.TryParse(timestampElement.GetString(), out var t) ? t : 0;

    //// 假设 timestamp 为毫秒级,得到本地时间
    //var localTime = DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime;
    //columns.Add("time");
    //    values.Add($"'{localTime:yyyy-MM-dd HH:mm:ss.fff}'");

    //    // 仍然插入原始 timestamp 字段
    //    columns.Add("timestamp");
    //    values.Add(ConvertToSqlValue(timestampElement));
    //}

    //    //// 添加其他字段
    //    //foreach (var kvp in dataDict)
    //    //{
    //    //    if (kvp.Key.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
    //    //        continue;

    //    //    var columnName = SanitizeColumnName(kvp.Key);
    //    //    // 统一转字符串
    //    //    values.Add($"'{kvp.Value.ToString().Replace("'", "''")}'");
    //    //    columns.Add(columnName);
    //    //}

    //    //var insertSql = $@"
    //    //INSERT INTO {tableName} ({string.Join(", ", columns)})
    //    //VALUES ({string.Join(", ", values)})";

    //    //await using var cmd = new NpgsqlCommand(insertSql, conn);
    //    //await cmd.ExecuteNonQueryAsync(token);
    //}

    private static async Task InsertDataAsync(
    NpgsqlConnection conn,
    string tableName,
    Dictionary<string, JsonElement> dataDict,
    CancellationToken token)
    {
        var columns = new List<string>();
        var values = new List<string>();

        // 添加 utc_timestamp 字段（UTC时间字符串）
        columns.Add("utc_timestamp");
        values.Add($"'{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}'");

        // 处理 timestamp 字段，并生成 time 字段
        if (dataDict.TryGetValue("timestamp", out var timestampElement))
        {
            // 转换 timestamp 为本地时间字符串
            long ts = timestampElement.ValueKind == JsonValueKind.Number
                ? timestampElement.GetInt64()
                : long.TryParse(timestampElement.GetString(), out var t) ? t : 0;

            var localTime = DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime;

            // time: 本地时间字符串
            columns.Add("time");
            values.Add($"'{localTime:yyyy-MM-dd HH:mm:ss.fff}'");

            // timestamp: long，不加引号
            columns.Add("timestamp");
            values.Add(ts.ToString());

            dataDict.Remove("timestamp");
        }

        // 其他字段全部作为字符串插入
        foreach (var kvp in dataDict)
        {
            var columnName = SanitizeColumnName(kvp.Key);
            var value = kvp.Value;
            string sqlValue = value.ValueKind switch
            {
                JsonValueKind.String => $"'{value.GetString()?.Replace("'", "''")}'",
                JsonValueKind.Number => $"'{value.ToString()}'",
                JsonValueKind.True => "'true'",
                JsonValueKind.False => "'false'",
                JsonValueKind.Null => "null",
                _ => $"'{value.GetRawText().Replace("'", "''")}'"
            };
            values.Add(sqlValue);
            columns.Add(columnName);
        }

        var insertSql = $@"
        INSERT INTO {tableName} ({string.Join(", ", columns)})
        VALUES ({string.Join(", ", values)})";

        await using var cmd = new NpgsqlCommand(insertSql, conn);
        await cmd.ExecuteNonQueryAsync(token);
    }

    /// <summary>
    /// 转换为 SQL 值
    /// </summary>
    private static string ConvertToSqlValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out var longVal)
                ? longVal.ToString()
                : element.GetDouble().ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => $"'{element.GetString()?.Replace("'", "''")}'",
            JsonValueKind.Null => "null",
            _ => $"'{element.GetRawText().Replace("'", "''")}'"
        };
    }

    private static string SanitizeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return string.Empty;

        tableName = tableName.Trim();  // 移除前后空格

        // 只替换非法字符为下划线，保留字母、数字、下划线，保持大小写
        var sanitized = new string(tableName
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray());

        // 如果清理后为空，返回空字符串
        return string.IsNullOrEmpty(sanitized) ? string.Empty : sanitized;
    }

    private static string SanitizeColumnName(string columnName)
    {
        return new string(columnName
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray()).ToLower();
    }
}