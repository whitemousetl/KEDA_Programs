using KEDA_Receiver.Extensions;
using KEDA_Receiver.Models;
using KEDA_Receiver.Services;
using KEDA_Share.Entity;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KEDA_Receiver;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-receiver-.txt"),
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

        builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
        builder.Services.AddSingleton<MongoDbContext>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<MongoSettings>>();
            var connStr = config.Value.ConnectionString ?? "mongodb://keda:keda_admin_2024@localhost:27017/StationConfiguration/?authSource=admin&replicaSet=rs0&authMechanism=SCRAM-SHA-256&readPreference=primary&directConnection=true";
            return new MongoDbContext(connStr);
        });
        builder.Services.AddSingleton<IWorkstationRepository>(sp =>
        {
            var mongoSettings = sp.GetRequiredService<MongoSettings>();
            var dbContext = sp.GetRequiredService<MongoDbContext>();
            var collection = dbContext.GetCollection<Workstation>(mongoSettings.ConfigDb, mongoSettings.ConfigCollection);
            return new WorkstationRepository(collection);
        });
        builder.Services.AddScoped<IValidator<Protocol>, ProtocolValidator>();
        builder.Services.AddScoped<IValidator<Device>, DeviceValidator>();
        builder.Services.AddScoped<IValidator<Point>, PointValidator>();
        builder.Services.AddScoped<WorkstationConfigService>();

        builder.Services.AddAuthorization();

        var app = builder.Build();

        //��չ����
        app.UseGlobalExceptionHanlder();
        app.MapReceiverApis();

        app.UseAuthorization();

        app.Run();
    }
}
