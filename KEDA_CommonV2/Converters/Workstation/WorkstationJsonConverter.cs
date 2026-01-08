using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters.Workstation;
public class WorkstationJsonConverter : JsonConverter<WorkstationDto>
{
    public override WorkstationDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var idPropertyName = nameof(WorkstationDto.Id);
        var ipAddressPropertyName = nameof(WorkstationDto.IpAddress);
        var protocolsPropertyName = nameof(WorkstationDto.Protocols);

        //存在性校验
        JsonValidateHelper.EnsurePropertyExists(root, idPropertyName);
        JsonValidateHelper.EnsurePropertyExists(root, ipAddressPropertyName);
        JsonValidateHelper.EnsurePropertyExists(root, protocolsPropertyName);

        //字段类型校验
        var id = JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, idPropertyName, JsonValueKind.String);
        var ip = JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, ipAddressPropertyName, JsonValueKind.String);
        JsonValidateHelper.EnsurePropertyTypeIsRight<JsonElement>(root, protocolsPropertyName, JsonValueKind.Array);

        // 反序列化 Protocols 列表，自动走 ProtocolJsonConverter
        var protocols = root.TryGetProperty(protocolsPropertyName, out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.Deserialize<List<ProtocolDto>>(options) ?? []
            : [];

        //非必须存在字段，如果存在，校验其类型
        var name = JsonValidateHelper.GetOptionalValue<string>(root, nameof(WorkstationDto.Name), JsonValueKind.String) ?? string.Empty;

        return new WorkstationDto
        {
            Id = id,
            Name = name,
            IpAddress = ip,
            Protocols = protocols
        };
    }

    public override void Write(Utf8JsonWriter writer, WorkstationDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
