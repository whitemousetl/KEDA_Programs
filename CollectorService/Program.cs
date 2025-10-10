using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;

namespace CollectorService;

public class Program
{
    public static void Main(string[] args)
    {
        var projectName = "CollectorService";

        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
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
                var db = client.GetDatabase("DeviceStatus"); // ���ݿ���
                                                             // ����ֻ��Ĭ�ϼ��ϣ�ʵ����ʱ��̬��ȡ
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

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            builder.Services.AddSingleton<IWorkstationProvider, WorkstationProvider>();

            if (!ActiveHsl(builder.Configuration))
                return;

            var host = builder.Build();
            host.Run();

            Log.Information("����ʼִ�У�");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ӧ�ó�������ʱ������������");
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
            Log.Error("----------------------Hsl��֤ʧ��----------------------");
            return false;
        }
        else
        {
            Log.Information("----------------------Hsl��֤�ɹ�----------------------");
            return true;
        }
    }
}