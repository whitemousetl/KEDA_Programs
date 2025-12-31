namespace KEDA_CommonV2.Configuration;

public class MqttTopicSettings
{
    public string ControlPrefix { get; set; } = string.Empty;
    public string ProtocolWritePrefix { get; set; } = string.Empty;
    public string WorkstationDataPrefix { get; set; } = string.Empty;
    public string WorkstationStatusPrefix { get; set; } = string.Empty;
    public string ProtocolWriteResultPrefix { get; set; } = string.Empty;
    public string WorkstationConfigSendPrefix { get; set; } = string.Empty;
    public string WorkstationConfigResponsePrefix { get; set; } = string.Empty;
}