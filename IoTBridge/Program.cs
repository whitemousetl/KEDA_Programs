using IoTBridge.Extensions;
using IoTBridge.Services.Interfaces.Modbus;
using Serilog;
using System.Reflection;

namespace IoTBridge;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var projectNmae = Assembly.GetExecutingAssembly().GetName().Name;

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", Serilog.Events.LogEventLevel.Fatal)
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "Logs", $"log-{projectNmae}-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        });

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var kestrelConfig = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelConfig);
        });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (!ActiveHsl(builder.Configuration)) return;

        app.UseWebSockets();

        app.UseAuthorization();

        app.MapIotBridgeApis();
        app.MapIotBridgeWebSockets();
        app.UseGlobalExceptionHanlder();

        app.Run();
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