using CollectorService.Services;
using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Services;
using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using KEDA_CommonV2.Extensions;
using Serilog;
using Serilog.Events;

namespace CollectorService;

public class Program
{
    public static void Main(string[] args)
    {
        var projectName = "CollectorService";

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

            // ========== 配置加载顺序 ==========
            // 1. 首先加载共享配置
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddSharedConfiguration(builder.Environment.EnvironmentName);

            // 2. 然后加载本地配置（会覆盖共享配置）
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (args != null)
            {
                builder.Configuration.AddCommandLine(args);
            }
            // ==================================

            //初始化配置
            SharedConfigHelper.Init(builder.Configuration);

            builder.Services.AddHostedService<Worker>();
            builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
            builder.Services.AddSingleton<IMongoDbContext<Workstation>>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<MongoSettings>>();
                var connStr = config.Value.ConnectionString ?? "mongodb://keda:keda_admin_2025@localhost:27017/StationConfiguration/?authSource=admin&replicaSet=rs0&authMechanism=SCRAM-SHA-256&readPreference=primary&directConnection=true";
                var client = new MongoClient(connStr);
                var db = client.GetDatabase(config.Value.ConfigDb);
                var collection = db.GetCollection<Workstation>(config.Value.ConfigCollection);
                return new MongoDbContext<Workstation>(collection);
            });

            builder.Services.AddSingleton<IMongoDbContext<DeviceStatus>>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<MongoSettings>>();
                var connStr = config.Value.ConnectionString ?? "mongodb://keda:keda_admin_2025@localhost:27017/StationConfiguration/?authSource=admin&replicaSet=rs0&authMechanism=SCRAM-SHA-256&readPreference=primary&directConnection=true";
                var client = new MongoClient(connStr);
                var db = client.GetDatabase("DeviceStatus"); // 数据库名
                                                             // 这里只是默认集合，实际用时动态获取
                var collection = db.GetCollection<DeviceStatus>("default");
                return new MongoDbContext<DeviceStatus>(collection);
            });

            builder.Services.AddSingleton<IWorkstationRepository>(sp =>
            {
                var context = sp.GetRequiredService<IMongoDbContext<Workstation>>();
                return new WorkstationRepository(context);
            });

            builder.Services.AddSingleton<IDeviceStatusRepository>(sp =>
            {
                var context = sp.GetRequiredService<IMongoDbContext<DeviceStatus>>();
                return new DeviceStatusRepository(context);
            });

            builder.Services.AddSingleton<IDeviceResultRepository>(sp =>
            {
                var context = sp.GetRequiredService<IMongoDbContext<DeviceStatus>>();
                return new DeviceResultRepository(context);
            });

            builder.Services.AddSingleton<IWorkstationProvider, WorkstationProvider>();

            // 添加缺失的服务注册
            builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>();
            builder.Services.AddSingleton<IMqttPublishManager, MqttPublishManager>();
            builder.Services.AddSingleton<IEquipmentDataProcessor, EquipmentDataProcessor>();
            builder.Services.AddSingleton<IPointExpressionConverter, PointExpressionConverter>();
            builder.Services.AddSingleton<IVirtualPointCalculator, VirtualPointCalculator>();

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