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
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var labelPropertyName = nameof(ParameterDto.Label);

        //存在性校验
        JsonValidateHelper.EnsurePropertyExists(root, labelPropertyName);

        //字段类型校验
        var label = JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, labelPropertyName, JsonValueKind.String);

        //Name如果存在，必须为字符串
        var stationNo = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.StationNo), JsonValueKind.String) ?? string.Empty; 
        var dataType = JsonValidateHelper.GetOptionalEnum<DataType>(root, nameof(ParameterDto.DataType)) ?? DataType.Unknown;
        var address = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.Address), JsonValueKind.String) ?? string.Empty;
        var length = JsonValidateHelper.GetOptionalValue<ushort>(root, nameof(ParameterDto.Length), JsonValueKind.Number);
        var defaultValue = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.DefaultValue), JsonValueKind.String) ?? string.Empty;
        var cycle = JsonValidateHelper.GetOptionalValue<int>(root, nameof(ParameterDto.Cycle), JsonValueKind.Number);
        var positiveExpression = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.PositiveExpression), JsonValueKind.String) ?? string.Empty;
        var minValue = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.MinValue), JsonValueKind.String) ?? string.Empty;
        var maxValue = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.MaxValue), JsonValueKind.String) ?? string.Empty;
        var dataFormat = JsonValidateHelper.GetOptionalEnum<DataFormat>(root, nameof(ParameterDto.DataFormat)) ?? 0;
        var addressStartWithZero = JsonValidateHelper.GetOptionalValue<bool>(root, nameof(ParameterDto.AddressStartWithZero), JsonValueKind.True);
        var instrumentType = JsonValidateHelper.GetOptionalEnum<InstrumentType>(root, nameof(ParameterDto.InstrumentType)) ??  InstrumentType.Unknown;
        var value = JsonValidateHelper.GetOptionalValue<string>(root, nameof(ParameterDto.Value), JsonValueKind.String) ?? string.Empty;

        return new ParameterDto
        {
            Label = label,
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
