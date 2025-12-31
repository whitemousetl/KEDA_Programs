using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters;

public class ProtocolJsonConverter : JsonConverter<Protocol>
{
    public override Protocol? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Interface", out var interfaceProp))
            throw new JsonException("缺少Interface字段，无法确定协议类型");

        ProtocolInterface protocolInterface;
        if (interfaceProp.ValueKind == JsonValueKind.Number)
        {
            var interfaceInt = interfaceProp.GetInt32();
            protocolInterface = (ProtocolInterface)interfaceInt;
        }
        else
            throw new JsonException("Interface字段类型错误，必须为数字或字符串");

        return protocolInterface switch
        {
            ProtocolInterface.LAN => root.Deserialize<LanProtocol>(options),
            ProtocolInterface.COM => root.Deserialize<SerialProtocol>(options),
            ProtocolInterface.API => root.Deserialize<ApiProtocol>(options),
            ProtocolInterface.DATABASE => root.Deserialize<DatabaseProtocol>(options),
            _ => throw new JsonException($"不支持的协议类型: {protocolInterface}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Protocol value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}