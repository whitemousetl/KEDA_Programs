using KEDA_Share.Enums;
using Newtonsoft.Json;

namespace IoTBridge.Models.ProtocolResponses;

public class ModbusRtuResponse
{
    public ProtocolStatus ProtocolStatus { get; set; }
    public string? ErrorMessage {  get; set; }
    public List<ModbusRtuDeviceResponse> DeviceResponses { get; set; } = [];

}

public class ModbusRtuDeviceResponse
{
    public string DeviceId { get; set; } = string.Empty;
    [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
    public List<ReadValueBase> Values { get; set; } = [];
}

public abstract class ReadValueBase
{
    public bool IsSuccess { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class ReadValue<T> : ReadValueBase
{
    public T? Value { get; set; }
}