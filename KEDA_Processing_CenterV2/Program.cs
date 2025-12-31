using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Data.Initialization;
using KEDA_CommonV2.Extensions;
using KEDA_CommonV2.Interfaces;
using KEDA_CommonV2.Services;
using KEDA_CommonV2.Utilities;
using KEDA_Processing_CenterV2.Interfaces;
using KEDA_Processing_CenterV2.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace KEDA_Processing_CenterV2;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        #region 合并共享配置和本地配置
        // ========== 配置加载顺序 ==========
        // 1. 首先加载共享配置
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddSharedConfiguration(builder.Environment.EnvironmentName);

        // 2. 然后加载本地配置（会覆盖共享配置）
        builder.Configuration
            .AddJsonFile("appsettings. json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (args != null)
        {
            builder.Configuration.AddCommandLine(args);
        }
        // ================================== 
        #endregion

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
        builder.Services.AddHostedService<Worker>(); //后台线程：初始化订阅 + 监控配置变化
        builder.Services.AddSingleton<IMqttPublishService, MqttPublishService>(); //订阅服务
        builder.Services.AddSingleton<IMqttSubscribeService, MqttSubscribeService>(); //发布服务
        builder.Services.AddScoped<IMqttPublishManager, MqttPublishManager>(); //发布管理，部分主题通过桥接发布到服务器
        builder.Services.AddScoped<IMqttSubscribeManager, MqttSubscribeManager>(); //订阅管理
        builder.Services.AddScoped<IConfigMonitor, ConfigMonitor>(); //监控配置服务
        builder.Services.AddScoped<IDeviceDataProcessor, DeviceDataProcessor>(); //数据处理服务
        builder.Services.AddScoped<IVirtualPointCalculator, VirtualPointCalculator>(); //虚拟点计算服务
        builder.Services.AddScoped<IPointExpressionConverter, PointExpressionConverter>(); //表达式计算服务
        builder.Services.AddScoped<IDeviceNotificationService, DeviceNotificationService>(); //监控设备状态服务，发布设备状态
        builder.Services.AddSingleton<IDeviceDataStorageService, QuestDbDeviceDataStorageService>(); //QuestDb数据存储服务，本地存储，TTL根据配置更改
        builder.Services.AddScoped<IWorkstationConfigProvider, QuestWorkstationConfigProvider>(); //QuestDb工作站配置存储服务,无TTL

        #endregion

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
        #endregion

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
}