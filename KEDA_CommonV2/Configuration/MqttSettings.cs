using KEDA_CommonV2.ConfigurationInterface;

namespace KEDA_CommonV2.Configuration;

public class MqttSettings : IMqttSettings
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int ReconnectDelaySeconds { get; set; }
    public int MaxReconnectDelaySeconds { get; set; }
    public int MessageTimeoutSeconds { get; set; }
    public bool AutoReconnect { get; set; }
}