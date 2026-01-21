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

        var namePrefix = "Equipment的";

        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(EquipmentDto.Id),  JsonValueKind.String);

        var isCollect = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(root, namePrefix, nameof(EquipmentDto.IsCollect),  JsonValueKind.True);

        var parameters = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<ParameterDto>>(root, namePrefix, nameof(EquipmentDto.Parameters),  JsonValueKind.Array);

        var equipmentType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EquipmentType>(root, namePrefix, nameof(EquipmentDto.EquipmentType));

        //Name如果存在，必须为字符串
        var name = JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(WorkstationDto.Name), JsonValueKind.String) ?? string.Empty;

        return new EquipmentDto
        {
            Id = id,
            IsCollect = isCollect,
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
