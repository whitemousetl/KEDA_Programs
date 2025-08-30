using KEDA_Receiver.Extensions;
using KEDA_Receiver.Models;
using KEDA_Receiver.Services;
using KEDA_Share.Entity;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using System.Reflection;

namespace KEDA_Receiver;

public class Program
{
    public static void Main(string[] args)
    {
        var projectNmae = Assembly.GetExecutingAssembly().GetName().Name;

        var builder = WebApplication.CreateBuilder(args);

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

        builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
        builder.Services.AddSingleton<IMongoDbContext<Workstation>>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<MongoSettings>>();
            var connStr = config.Value.ConnectionString ?? "mongodb://keda:keda_admin_2024@localhost:27017/StationConfiguration/?authSource=admin&replicaSet=rs0&authMechanism=SCRAM-SHA-256&readPreference=primary&directConnection=true";
            var client = new MongoClient(connStr);
            var db = client.GetDatabase(config.Value.ConfigDb);
            var collection = db.GetCollection<Workstation>(config.Value.ConfigCollection);
            return new MongoDbContext<Workstation>(collection);
        });
        builder.Services.AddSingleton<IWorkstationRepository>(sp =>
        {
            var context = sp.GetRequiredService<IMongoDbContext<Workstation>>();
            return new WorkstationRepository(context);
        });
        builder.Services.AddScoped<IValidator<Workstation>, WorkstationValidator>();
        builder.Services.AddScoped<IValidator<Protocol>, ProtocolValidator>();
        builder.Services.AddScoped<IValidator<Device>, DeviceValidator>();
        builder.Services.AddScoped<IValidator<Point>, PointValidator>();
        builder.Services.AddScoped<WorkstationConfigService>();

        builder.Services.AddAuthorization();

        var app = builder.Build();

        //À©Õ¹·½·¨
        app.UseGlobalExceptionHanlder();
        app.MapReceiverApis();

        app.UseAuthorization();

        app.Run();
    }
}