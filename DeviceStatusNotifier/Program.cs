using KEDA_Share.Entity;
using KEDA_Share.Model;
using KEDA_Share.Repository.Implementations;
using KEDA_Share.Repository.Interfaces;
using KEDA_Share.Repository.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DeviceStatusNotifier;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
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

        builder.Services.AddSingleton<IMongoDbContext<KEDA_Share.Entity.DeviceStatus>>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<MongoSettings>>();
            var connStr = config.Value.ConnectionString ?? "mongodb://keda:keda_admin_2025@localhost:27017/StationConfiguration/?authSource=admin&replicaSet=rs0&authMechanism=SCRAM-SHA-256&readPreference=primary&directConnection=true";
            var client = new MongoClient(connStr);
            var db = client.GetDatabase("DeviceStatus");
            var collection = db.GetCollection<KEDA_Share.Entity.DeviceStatus>("default");
            return new MongoDbContext<KEDA_Share.Entity.DeviceStatus>(collection);
        });

        builder.Services.AddSingleton<IWorkstationRepository>(sp =>
        {
            var context = sp.GetRequiredService<IMongoDbContext<Workstation>>();
            return new WorkstationRepository(context);
        });

        builder.Services.AddSingleton<IDeviceStatusRepository>(sp =>
        {
            var context = sp.GetRequiredService<IMongoDbContext<KEDA_Share.Entity.DeviceStatus>>();
            return new DeviceStatusRepository(context);
        });

        builder.Services.AddSingleton<IWorkstationProvider, WorkstationProvider>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}