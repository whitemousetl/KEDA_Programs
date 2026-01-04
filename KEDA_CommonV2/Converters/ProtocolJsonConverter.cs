using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters;

public class ProtocolJsonConverter : JsonConverter<ProtocolDto>
{
    public override ProtocolDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("InterfaceType", out var interfaceProp))
            throw new JsonException("缺少Interface字段，无法确定协议类型");

        InterfaceType interfaceType;
        if (interfaceProp.ValueKind == JsonValueKind.Number)
        {
            var interfaceInt = interfaceProp.GetInt32();
            interfaceType = (InterfaceType)interfaceInt;
        }
        else
            throw new JsonException("Interface字段类型错误，必须为数字或字符串");

        return interfaceType switch
        {
            InterfaceType.LAN => root.Deserialize<LanProtocolDto>(options),
            InterfaceType.COM => root.Deserialize<SerialProtocolDto>(options),
            InterfaceType.API => root.Deserialize<ApiProtocolDto>(options),
            InterfaceType.DATABASE => root.Deserialize<DatabaseProtocolDto>(options),
            _ => throw new JsonException($"不支持的协议类型: {interfaceType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ProtocolDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}