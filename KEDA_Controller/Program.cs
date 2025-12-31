using KEDA_Common.Entity;
using KEDA_Common.Helper;
using KEDA_Common.Interfaces;
using KEDA_Common.Services;
using KEDA_Controller.Interfaces;
using KEDA_Controller.Services;
using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Events;
using SqlSugar;
using System.Reflection;

namespace KEDA_Controller;

public class Program
{
    public static void Main(string[] args)
    {
        var projectName = Assembly.GetExecutingAssembly().GetName().Name;

        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Error()
           .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal)
           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
           .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
           .WriteTo.File(
               path: Path.Combine(AppContext.BaseDirectory, "Logs", $"log-{projectName}-.txt"),
               rollingInterval: RollingInterval.Day,
               retainedFileCountLimit: 7,
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
           .CreateLogger();

        try
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            var connectionString = builder.Configuration.GetConnectionString("WorkstationDb");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("未配置数据库连接字符串（WorkstationDb）。请检查 appsettings.json 或环境变量。");
            DbInitializer.EnsureDatabaseAndTables(connectionString, DbType.MySql);

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
            builder.Services.AddSingleton<IWriteTaskLogService, WriteTaskLogService>();
            builder.Services.AddSingleton<IProtocolTaskManager, ProtocolTaskManager>();
            builder.Services.AddSingleton<IWriteTaskManager, WriteTaskManager>();

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            if (!ActiveHsl(builder.Configuration))
                return;

            var host = builder.Build();
            Log.Information("程序开始执行！");
            host.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动时发生致命错误");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static bool ActiveHsl(ConfigurationManager configuration)
    {
        var hslAuthCode = configuration["HslCommunication:Auth"];
        if (!HslCommunication.Authorization.SetAuthorizationCode(hslAuthCode))
        {
            Log.Error("----------------------Hsl验证失败----------------------");
            return false;
        }
        else
        {
            Log.Information("----------------------Hsl验证成功----------------------");
            return true;
        }
    }
}