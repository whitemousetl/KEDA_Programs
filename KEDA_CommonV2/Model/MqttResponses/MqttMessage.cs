using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Model.MqttResponses;

public class MqttMessage<T>
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
    [JsonPropertyName("time")]
    public string Time { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    [JsonPropertyName("payload")]
    public T Payload { get; set; } = default!;
}
