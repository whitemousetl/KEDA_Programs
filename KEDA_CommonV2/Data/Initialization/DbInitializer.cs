using KEDA_CommonV2.Configuration;
using Npgsql;

namespace KEDA_CommonV2.Data.Initialization;

public static class DbInitializer
{
    public static async Task EnsureQuestDbTablesAsync(
    DatabaseSettings dbSettings,
    CancellationToken token = default)
    {
        var connectionString = dbSettings.QuestDb;
        var configTableName = dbSettings.ConfigTableName;
        var writeLogTableName = dbSettings.WriteLogTableName;
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(configTableName) || string.IsNullOrWhiteSpace(writeLogTableName))
            throw new InvalidOperationException("未配置数据库连接字符串（WorkstationDb）。请检查 appsettings.json 或环境变量。");

        // 1. WorkstationConfigEntity
        await EnsureQuestDbTableExistsAsync(
            connectionString,
            configTableName,
            $@"
            CREATE TABLE {configTableName} (
                ConfigJson STRING,
                SaveTime TIMESTAMP,
                SaveTimeLocal STRING   -- 东八区时间字符串
            ) TIMESTAMP(SaveTime) PARTITION BY MONTH",
            token);

        // 2. WriteTaskLog
        await EnsureQuestDbTableExistsAsync(
            connectionString,
            writeLogTableName,
             $@"
            CREATE TABLE {writeLogTableName} (
                UUID STRING,
                DeviceType INT,
                WriteTaskJson STRING,
                Time TIMESTAMP,
                TimeLocal STRING,   -- 东八区时间字符串
                IsSuccess BOOLEAN,
                Msg STRING
            ) TIMESTAMP(Time) PARTITION BY MONTH",
            token);
    }

    private static async Task EnsureQuestDbTableExistsAsync(
        string connectionString,
        string tableName,
        string createTableSql,
        CancellationToken token = default)
    {
        var checkTableSql = $@"SELECT count(*) FROM tables() WHERE table_name = '{tableName}'";

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(token);

        await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(token));
        if (count == 0)
        {
            await using var createCmd = new NpgsqlCommand(createTableSql, conn);
            await createCmd.ExecuteNonQueryAsync(token);
        }
    }
}