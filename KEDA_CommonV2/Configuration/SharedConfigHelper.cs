using KEDA_CommonV2.Enums;
using Microsoft.Extensions.Configuration;

namespace KEDA_CommonV2.Configuration;

public static class SharedConfigHelper
{
    private static IConfiguration? _configuration;

    public static void Init(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static DatabaseSettings DatabaseSettings =>
        _configuration?.GetSection("DatabaseSettings").Get<DatabaseSettings>()
        ?? throw new InvalidOperationException("DatabaseSettings 配置未找到或格式错误");

    public static EquipmentDataStorageSettings EquipmentDataStorageSettings =>
        _configuration?.GetSection("EquipmentDataStorageSettings").Get<EquipmentDataStorageSettings>()
        ?? throw new InvalidOperationException("EquipmentDataStorageSettings 配置未找到或格式错误");

    public static MqttTopicSettings MqttTopicSettings =>
        _configuration?.GetSection("MqttTopicSettings").Get<MqttTopicSettings>()
        ?? throw new InvalidOperationException("MqttTopicSettings 配置未找到或格式错误");

    public static MqttSettings MqttSettings =>
        _configuration?.GetSection("MqttSettings").Get<MqttSettings>()
        ?? throw new InvalidOperationException("MqttSettings 配置未找到或格式错误");

    // 新增：HslCommunication
    public static HslCommunicationSettings HslCommunicationSettings =>
        _configuration?.GetSection("HslCommunication").Get<HslCommunicationSettings>()
        ?? throw new InvalidOperationException("HslCommunication 配置未找到或格式错误");

    // 新增：Collector
    public static CollectorSettings CollectorSettings =>
        _configuration?.GetSection("Collector").Get<CollectorSettings>()
        ?? throw new InvalidOperationException("Collector 配置未找到或格式错误");

    // 新增：SerialProtocol
    public static HashSet<ProtocolType> SerialLikeProtocols =>
     _configuration?
         .GetSection("SerialLikeProtocols")
         .Get<List<string>>()?
         .Select(x => Enum.TryParse<ProtocolType>(x, out var pt) ? pt : (ProtocolType?)null)
         .Where(x => x.HasValue)
         .Select(x => x!.Value)
         .ToHashSet()
     ?? throw new InvalidOperationException("SerialLikeProtocols 配置未找到或格式错误");
}