using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using Microsoft.Extensions.Configuration;
using MQTTnet.Client;

namespace KEDA_CommonV2.Configuration;

public class SharedConfigHelper : ISharedConfigHelper
{
    public SharedConfigHelper(IConfiguration configuration)
    {
        DatabaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>()
            ?? throw new InvalidOperationException("DatabaseSettings 配置未找到或格式错误");

        EquipmentDataStorageSettings = configuration.GetSection("EquipmentDataStorageSettings").Get<EquipmentDataStorageSettings>()
            ?? throw new InvalidOperationException("EquipmentDataStorageSettings 配置未找到或格式错误");

        MqttTopicSettings = configuration.GetSection("MqttTopicSettings").Get<MqttTopicSettings>()
            ?? throw new InvalidOperationException("MqttTopicSettings 配置未找到或格式错误");

        MqttSettings = configuration.GetSection("MqttSettings").Get<MqttSettings>()
            ?? throw new InvalidOperationException("MqttSettings 配置未找到或格式错误");

        HslCommunicationSettings = configuration.GetSection("HslCommunication").Get<HslCommunicationSettings>()
            ?? throw new InvalidOperationException("HslCommunication 配置未找到或格式错误");

        CollectorSettings = configuration.GetSection("Collector").Get<CollectorSettings>()
            ?? throw new InvalidOperationException("Collector 配置未找到或格式错误");

        var serialNames = configuration.GetSection("SerialLikeProtocols").Get<List<string>>()
            ?? throw new InvalidOperationException("SerialLikeProtocols 配置未找到或格式错误");

        SerialLikeProtocols = serialNames
            .Select(x => Enum.TryParse<ProtocolType>(x, out var pt) ? pt : (ProtocolType?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();
    }

    public DatabaseSettings DatabaseSettings { get; }
    public EquipmentDataStorageSettings EquipmentDataStorageSettings { get; }
    public MqttTopicSettings MqttTopicSettings { get; }
    public MqttSettings MqttSettings { get; }
    public HslCommunicationSettings HslCommunicationSettings { get; }
    public CollectorSettings CollectorSettings { get; }
    public HashSet<ProtocolType> SerialLikeProtocols { get; }

    public MqttClientOptions MqttClientOptions => new MqttClientOptionsBuilder()
            .WithTcpServer(MqttSettings.Server, MqttSettings.Port)
            .WithCredentials(MqttSettings.Username, MqttSettings.Password)
            .WithClientId($"keda-publisher-{Environment.MachineName}-{Guid.NewGuid():N}")
            .WithCleanSession(true)  // ← 发布者应使用 CleanSession
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(10))
            .Build();
}