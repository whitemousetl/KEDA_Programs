using KEDA_Common.Entity;
using KEDA_Common.Interfaces;
using KEDA_Common.Services;
using KEDA_Controller.Interfaces;
using KEDA_Controller.Services;
using Microsoft.Data.Sqlite;
using SqlSugar;

namespace KEDA_Controller;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        #region 初始化Sqlite数据库
        var rawPath = builder.Configuration.GetConnectionString("WorkstationDb");
        var connectionString = $"Data Source={rawPath}";

        // 只在应用启动时执行一次
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode;";
            var currentMode = cmd.ExecuteScalar()?.ToString();
            if (currentMode != "wal" && currentMode != "WAL")
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();
            }
        }

        // 表初始化
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        db.CodeFirst.InitTables<WorkstationConfig>();
        db.CodeFirst.InitTables<ProtocolConfig>();
        db.CodeFirst.InitTables<WritePointEntity>();
        db.CodeFirst.InitTables<ProtocolData>();
        #endregion

        // 注入SqlSugarClient
        builder.Services.AddTransient(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            });
        });

        builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>();
        builder.Services.AddSingleton<IMqttSubscribeService, MqttSubscribeService>();
        builder.Services.AddSingleton<IProtocolConfigProvider, ProtocolConfigProvider>();
        builder.Services.AddSingleton<IProtocolTaskManager, ProtocolTaskManager>();
        builder.Services.AddSingleton<IWriteTaskManager, WriteTaskManager>();

        var host = builder.Build();
        host.Run();
    }
}