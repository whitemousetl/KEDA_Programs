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
        var namePrefix = "Workstatoin的";

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        //Id
        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationDto.Id), JsonValueKind.String);
        //IpAddress
        var ip = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationDto.IpAddress), JsonValueKind.String);
        //协议列表
        var protocols = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<ProtocolDto>>(root, namePrefix, nameof(WorkstationDto.Protocols), JsonValueKind.Array);
        //Name
        var name = JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(WorkstationDto.Name), JsonValueKind.String);

        return new WorkstationDto
        {
            Id = id,
            Name = name ?? string.Empty,
            IpAddress = ip,
            Protocols = protocols
        };
    }

    public override void Write(Utf8JsonWriter writer, WorkstationDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
