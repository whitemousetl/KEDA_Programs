using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Data.Initialization;
using KEDA_CommonV2.Extensions;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Services;
using KEDA_CommonV2.Utilities;
using KEDA_ControllerV2.Interfaces;
using KEDA_ControllerV2.Services;
using Serilog;

namespace KEDA_ControllerV2;

public class Program
{
    public static async Task Main(string[] args)
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

        // 配置 Serilog
        var localIp = SystemMsg.GetLocalIp();

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.WithProperty("LocalIp", localIp)
                .Enrich.WithProperty("AppName", "KEDA_Processing_CenterV2");
        });

        #region 依赖注入

        builder.Services.AddHostedService<Worker>();
        builder.Services.AddSingleton<IDeviceDataStorageService, QuestDbDeviceDataStorageService>();
        builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>();
        builder.Services.AddSingleton<IMqttSubscribeService, MqttSubscribeService>();
        builder.Services.AddSingleton<IWorkstationConfigProvider, QuestWorkstationConfigProvider>();
        builder.Services.AddScoped<IWriteTaskLogService, WriteTaskLogService>();

        builder.Services.AddScoped<IConfigMonitor, ConfigMonitor>();
        builder.Services.AddScoped<IDeviceDataProcessor, DeviceDataProcessor>(); //数据处理服务
        builder.Services.AddScoped<IDeviceNotificationService, DeviceNotificationService>(); //监控设备状态服务，发布设备状态
        builder.Services.AddScoped<IMqttPublishManager, MqttPublishManager>();
        builder.Services.AddScoped<IMqttSubscribeManager, MqttSubscribeManager>();
        builder.Services.AddScoped<IPointExpressionConverter, PointExpressionConverter>(); //表达式计算服务
        builder.Services.AddScoped<IProtocolTaskManager, ProtocolTaskManager>();
        builder.Services.AddScoped<IVirtualPointCalculator, VirtualPointCalculator>(); //虚拟点计算服务
        builder.Services.AddScoped<IWriteTaskManager, WriteTaskManager>();

        #endregion 依赖注入

        if (!ActiveHsl(builder.Configuration))
            return;

        var app = builder.Build();

        #region 配置并初始化QuestDB数据库

        using (var scope = app.Services.CreateScope())
        {
            //初始化WorkstationConfig,WriteTaskLog
            await DbInitializer.EnsureQuestDbTablesAsync(SharedConfigHelper.DatabaseSettings, CancellationToken.None);
            //初始化questdb的设备表的TTL，不包括WorkstationConfig,WriteTaskLog
            var questdbService = scope.ServiceProvider.GetRequiredService<IDeviceDataStorageService>();
            await questdbService.EnsureAllTablesTtlUpdatedAsync();
        }

        #endregion 配置并初始化QuestDB数据库

        try
        {
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动时发生致命错误");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();  // 确保日志刷新
        }
    }

    private static bool ActiveHsl(ConfigurationManager configuration)
    {
        var hslAuthCode = SharedConfigHelper.HslCommunicationSettings.Auth;
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