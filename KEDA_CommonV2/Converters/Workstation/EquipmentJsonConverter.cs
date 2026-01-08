using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters.Workstation;
public class EquipmentJsonConverter : JsonConverter<EquipmentDto>
{
    public override EquipmentDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        //存在性校验
        JsonValidateHelper.EnsurePropertyExists(root, nameof(EquipmentDto.Id));
        JsonValidateHelper.EnsurePropertyExists(root, nameof(EquipmentDto.EquipmentType));
        JsonValidateHelper.EnsurePropertyExists(root, nameof(EquipmentDto.Parameters));

        //字段类型校验
        var id = JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, nameof(EquipmentDto.Id), JsonValueKind.String);
        var equipmentType = JsonValidateHelper.EnsureEnumIsRight<EquipmentType>(root, nameof(EquipmentDto.EquipmentType));
        JsonValidateHelper.EnsurePropertyTypeIsRight<List<ParameterDto>>(root, nameof(EquipmentDto.Parameters), JsonValueKind.Array);

        //Name如果存在，必须为字符串
        var name = JsonValidateHelper.GetOptionalValue<string>(root, nameof(WorkstationDto.Name), JsonValueKind.String) ?? string.Empty;

        // 反序列化 Parameters 列表
        var parameters = root.TryGetProperty(nameof(EquipmentDto.Parameters), out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.Deserialize<List<ParameterDto>>(options) ?? []
            : [];

        return new EquipmentDto
        {
            Id = id,
            Name = name,
            EquipmentType = equipmentType,
            Parameters = parameters
        };
    }

    public override void Write(Utf8JsonWriter writer, EquipmentDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
