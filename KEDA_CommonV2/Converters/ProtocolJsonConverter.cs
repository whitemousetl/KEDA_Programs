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

        // Id校验
        RequireString("Id");

        // InterfaceType校验
        var interfaceType = (InterfaceType)RequireInt("InterfaceType");

        // ProtocolType校验
        RequireInt("ProtocolType");

        // 分派到具体协议类型
        return interfaceType switch
        {
            InterfaceType.LAN => root.Deserialize<LanProtocolDto>(options),
            InterfaceType.COM => root.Deserialize<SerialProtocolDto>(options),
            InterfaceType.API => root.Deserialize<ApiProtocolDto>(options),
            InterfaceType.DATABASE => root.Deserialize<DatabaseProtocolDto>(options),
            _ => throw new JsonException($"不支持的接口类型: {interfaceType}")
        };

        string RequireString(string name)
        {
            if(!root.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(prop.GetString()))
                throw new JsonException($"缺少或无效的{name}字段，必须为字符串");
            return prop.GetString()!;
        }

        int RequireInt(string name)
        {
            if(!root.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number)
                throw new JsonException($"缺少或无效的{name}字段，必须为数字");
            return prop.GetInt32();
        }
    }

    public override void Write(Utf8JsonWriter writer, ProtocolDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}