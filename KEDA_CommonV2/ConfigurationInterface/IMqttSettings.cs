using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.ConfigurationInterface;

public interface IMqttSettings
{
    public string Server { get; set; }
    public int Port { get; set; }
    public string Username { get; set; } 
    public string Password { get; set; } 
    public int ReconnectDelaySeconds { get; set; }
    public int MaxReconnectDelaySeconds { get; set; }
    public int MessageTimeoutSeconds { get; set; }
    public bool AutoReconnect { get; set; }
}
