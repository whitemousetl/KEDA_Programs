using KEDA_CommonV2.Configuration;
using KEDA_CommonV2.Enums;
using MQTTnet;
using MQTTnet.Client;

namespace KEDA_CommonV2.Interfaces;

public interface ISharedConfigHelper
{
    DatabaseSettings DatabaseSettings { get; }
    EquipmentDataStorageSettings EquipmentDataStorageSettings { get; }
    MqttTopicSettings MqttTopicSettings { get; }
    MqttSettings MqttSettings { get; }
    HslCommunicationSettings HslCommunicationSettings { get; }
    CollectorSettings CollectorSettings { get; }
    HashSet<ProtocolType> SerialLikeProtocols { get; }
    MqttClientOptions MqttClientOptions { get;  }
}
