using KEDA_CommonV2.Configuration;
using Npgsql;
using System.Text.RegularExpressions;

namespace KEDA_CommonV2.Data.Initialization;

public static partial class DbInitializer
{
    // 表名白名单正则（只允许字母、数字、下划线）
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex SafeTableNameRegex();

    public static async Task EnsureQuestDbTablesAsync(
    DatabaseSettings dbSettings,
    CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(dbSettings);

        var connectionString = dbSettings.QuestDb;
        var configTableName = dbSettings.ConfigTableName;
        var writeLogTableName = dbSettings.WriteLogTableName;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("未配置数据库连接字符串（QuestDb）。");

        ValidateTableName(configTableName, nameof(dbSettings.ConfigTableName));
        ValidateTableName(writeLogTableName, nameof(dbSettings.WriteLogTableName));

        // 复用同一个连接
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(token);

        await EnsureTableExistsAsync(conn, configTableName, BuildConfigTableSql(configTableName), token);
        await EnsureTableExistsAsync(conn, writeLogTableName, BuildWriteLogTableSql(writeLogTableName), token);
    }

    private static void ValidateTableName(string? tableName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", paramName);

        if (!SafeTableNameRegex().IsMatch(tableName))
            throw new ArgumentException($"表名 '{tableName}' 包含非法字符。", paramName);
    }

    private static string BuildConfigTableSql(string tableName) =>
       $"""
        CREATE TABLE {tableName} (
            ConfigJson STRING,
            SaveTime TIMESTAMP,
            SaveTimeLocal STRING
        ) TIMESTAMP(SaveTime) PARTITION BY MONTH
        """;

    private static string BuildWriteLogTableSql(string tableName) =>
        $"""
        CREATE TABLE {tableName} (
            UUID STRING,
            EquipmentType INT,
            WriteTaskJson STRING,
            Time TIMESTAMP,
            TimeLocal STRING,
            IsSuccess BOOLEAN,
            Msg STRING
        ) TIMESTAMP(Time) PARTITION BY MONTH
        """;

    private static async Task EnsureTableExistsAsync(
        NpgsqlConnection conn,
        string tableName,
        string createTableSql,
        CancellationToken token)
    {
        var checkTableSql = $"SELECT count(*) FROM tables() WHERE table_name = '{tableName}'";

        await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(token));

        if (count == 0)
        {
            await using var createCmd = new NpgsqlCommand(createTableSql, conn);
            await createCmd.ExecuteNonQueryAsync(token);
        }
    }
}