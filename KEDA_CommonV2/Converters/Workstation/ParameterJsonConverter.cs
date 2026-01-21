using HslCommunication.Core;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters.Workstation;
public class ParameterJsonConverter : JsonConverter<ParameterDto>
{
    public override ParameterDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var namePrefix = "Parameter的";
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        //存在性校验
        var label = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root,  namePrefix, nameof(ParameterDto.Label), JsonValueKind.String);
        var address = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ParameterDto.Address), JsonValueKind.String);
        var isMonitor = JsonValidateHelper.ValidateOptionalFields<bool>(root, namePrefix, nameof(ParameterDto.IsMonitor), JsonValueKind.True);

        //Name如果存在，必须为字符串
        var stationNo = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.StationNo),  JsonValueKind.String) ?? string.Empty; 
        var defaultValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.DefaultValue), JsonValueKind.String) ?? string.Empty;
        var positiveExpression = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.PositiveExpression), JsonValueKind.String) ?? string.Empty;
        var minValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.MinValue), JsonValueKind.String) ?? string.Empty;
        var maxValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.MaxValue), JsonValueKind.String) ?? string.Empty;
        var value = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterDto.Value), JsonValueKind.String) ?? string.Empty;
        var length = JsonValidateHelper.ValidateOptionalFields<ushort>(root, namePrefix, nameof(ParameterDto.Length), JsonValueKind.Number);
        var cycle = JsonValidateHelper.ValidateOptionalFields<int>(root, namePrefix, nameof(ParameterDto.Cycle), JsonValueKind.Number);
        var addressStartWithZero = JsonValidateHelper.ValidateOptionalFields<bool?>(root, namePrefix, nameof(ParameterDto.AddressStartWithZero), JsonValueKind.True);

        var dataType = JsonValidateHelper.GetOptionalEnum<DataType>(root, namePrefix, nameof(ParameterDto.DataType));
        var dataFormat = JsonValidateHelper.GetOptionalEnum<DataFormat>(root, namePrefix, nameof(ParameterDto.DataFormat));
        var instrumentType = JsonValidateHelper.GetOptionalEnum<InstrumentType>(root, namePrefix, nameof(ParameterDto.InstrumentType));

        return new ParameterDto
        {
            Label = label,
            IsMonitor = isMonitor,
            StationNo = stationNo,
            DataType = dataType,
            Address = address,
            Length = length,
            DefaultValue = defaultValue,
            Cycle = cycle,
            PositiveExpression = positiveExpression,
            MinValue = minValue,
            MaxValue = maxValue,
            DataFormat = dataFormat,
            AddressStartWithZero = addressStartWithZero,
            InstrumentType = instrumentType,
            Value = value,
        };
    }

    public override void Write(Utf8JsonWriter writer, ParameterDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
