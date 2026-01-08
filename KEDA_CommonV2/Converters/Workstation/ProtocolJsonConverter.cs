using HslCommunication.Core;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using System.Drawing;
using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KEDA_CommonV2.Converters.Workstation;

public class ProtocolJsonConverter : JsonConverter<ProtocolDto>
{
    public override ProtocolDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var idPropertyName = nameof(ProtocolDto.Id);
        var interfaceTypePropertyName = nameof(ProtocolDto.InterfaceType);
        var protocolTypePropertyName = nameof(ProtocolDto.ProtocolType);
        var equipmentsPropertyName = nameof(ProtocolDto.Equipments);

        //存在性校验
        JsonValidateHelper.EnsurePropertyExists(root, idPropertyName);
        JsonValidateHelper.EnsurePropertyExists(root, interfaceTypePropertyName);
        JsonValidateHelper.EnsurePropertyExists(root, protocolTypePropertyName);
        JsonValidateHelper.EnsurePropertyExists(root, equipmentsPropertyName);

        //字段类型校验
        JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, idPropertyName, JsonValueKind.String);
        var interfaceType = JsonValidateHelper.EnsureEnumIsRight<InterfaceType>(root, interfaceTypePropertyName);
        var protocolType = JsonValidateHelper.EnsureEnumIsRight<ProtocolType>(root, protocolTypePropertyName);
        JsonValidateHelper.EnsurePropertyTypeIsRight<List<EquipmentDto>>(root, equipmentsPropertyName, JsonValueKind.Array);

        //非必须存在字段，如果存在，校验其类型
        JsonValidateHelper.GetOptionalValue<int>(root, nameof(ProtocolDto.CollectCycle), JsonValueKind.Number);
        JsonValidateHelper.GetOptionalValue<int>(root, nameof(ProtocolDto.ReceiveTimeOut), JsonValueKind.Number);
        JsonValidateHelper.GetOptionalValue<int>(root, nameof(ProtocolDto.ConnectTimeOut), JsonValueKind.Number);
        JsonValidateHelper.GetOptionalValue<string>(root, nameof(ProtocolDto.Account), JsonValueKind.String);
        JsonValidateHelper.GetOptionalValue<string>(root, nameof(ProtocolDto.Password), JsonValueKind.String);
        JsonValidateHelper.GetOptionalValue<string>(root, nameof(ProtocolDto.Remark), JsonValueKind.String);
        JsonValidateHelper.GetOptionalValue<string>(root, nameof(ProtocolDto.AdditionalOptions), JsonValueKind.String);

        //字段间关系，根据接口类型限制协议类型，比如接口类型是LAN时，如果协议类型是ModbusRtu，则不被支持
        if (!ProtocolTypeHelper.IsProtocolTypeValidForInterface(interfaceType, protocolType))
            throw new JsonException($"接口类型{interfaceType}下不支持协议类型{protocolType}");

        // 跨对象校验：直接遍历JSON
        if (protocolType == ProtocolType.ModbusTcpNet
            || protocolType == ProtocolType.ModbusRtu
            || protocolType == ProtocolType.ModbusRtuOverTcp)
        {
            var equipments = root.GetProperty(nameof(ProtocolDto.Equipments));
            foreach (var equipment in equipments.EnumerateArray())
            {
                if (!equipment.TryGetProperty(nameof(EquipmentDto.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                    throw new JsonException("设备缺少参数列表");

                foreach (var parameter in parameters.EnumerateArray())
                {
                    // StationNo 必须存在且为字符串
                    if (!parameter.TryGetProperty(nameof(ParameterDto.StationNo), out var stationNoProp) || stationNoProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(stationNoProp.GetString()))
                        throw new JsonException("Modbus协议下，参数缺少StationNo或类型不正确");

                    // DataFormat 必须存在且为有效枚举
                    if (!parameter.TryGetProperty(nameof(ParameterDto.DataFormat), out var dataFormatProp) || dataFormatProp.ValueKind != JsonValueKind.Number)
                        throw new JsonException("Modbus协议下，参数缺少DataFormat或类型不正确");

                    // AddressStartWithZero 必须存在且为布尔
                    if (!parameter.TryGetProperty(nameof(ParameterDto.AddressStartWithZero), out var addrZeroProp) || (addrZeroProp.ValueKind != JsonValueKind.True && addrZeroProp.ValueKind != JsonValueKind.False))
                        throw new JsonException("Modbus协议下，参数缺少AddressStartWithZero或类型不正确");
                }
            }
        }

        // 子类特有字段存在性校验和字段类型校验
        switch (interfaceType)
        {
            case InterfaceType.LAN:
                //存在性校验
                JsonValidateHelper.EnsurePropertyExists(root, nameof(LanProtocolDto.IpAddress));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(LanProtocolDto.ProtocolPort));
                //字段类型校验
                JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, nameof(LanProtocolDto.IpAddress), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyTypeIsRight<int>(root, nameof(LanProtocolDto.ProtocolPort), JsonValueKind.Null);
                //非必须存在字段，如果存在，校验其类型
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(LanProtocolDto.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.COM:
                //存在性校验
                JsonValidateHelper.EnsurePropertyExists(root, nameof(SerialProtocolDto.SerialPortName));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(SerialProtocolDto.BaudRate));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(SerialProtocolDto.DataBits));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(SerialProtocolDto.Parity));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(SerialProtocolDto.StopBits));
                //字段类型校验
                JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, nameof(SerialProtocolDto.SerialPortName), JsonValueKind.String);
                JsonValidateHelper.EnsureEnumIsRight<BaudRateType>(root, nameof(SerialProtocolDto.BaudRate));
                JsonValidateHelper.EnsureEnumIsRight<DataBitsType>(root, nameof(SerialProtocolDto.DataBits));
                JsonValidateHelper.EnsureEnumIsRight<Parity>(root, nameof(SerialProtocolDto.Parity));
                JsonValidateHelper.EnsureEnumIsRight<StopBits>(root, nameof(SerialProtocolDto.StopBits));
                break;
            case InterfaceType.API:
                //存在性校验
                JsonValidateHelper.EnsurePropertyExists(root, nameof(ApiProtocolDto.AccessApiString));
                JsonValidateHelper.EnsurePropertyExists(root, nameof(ApiProtocolDto.RequestMethod));
                //字段类型校验
                JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, nameof(ApiProtocolDto.AccessApiString), JsonValueKind.String);
                JsonValidateHelper.EnsureEnumIsRight<RequestMethod>(root, nameof(ApiProtocolDto.RequestMethod));
                //非必须存在字段，如果存在，校验其类型
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(ApiProtocolDto.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.DATABASE:
                //存在性校验
                JsonValidateHelper.EnsurePropertyExists(root, nameof(DatabaseProtocolDto.QuerySqlString));
                //字段类型校验
                JsonValidateHelper.EnsurePropertyTypeIsRight<string>(root, nameof(DatabaseProtocolDto.QuerySqlString), JsonValueKind.String);
                //非必须存在字段，如果存在，校验其类型
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(DatabaseProtocolDto.IpAddress), JsonValueKind.String);
                JsonValidateHelper.GetOptionalValue<int>(root, nameof(DatabaseProtocolDto.ProtocolPort), JsonValueKind.Number);
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(DatabaseProtocolDto.DatabaseName), JsonValueKind.String);
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(DatabaseProtocolDto.DatabaseConnectString), JsonValueKind.String);
                JsonValidateHelper.GetOptionalValue<string>(root, nameof(DatabaseProtocolDto.Gateway), JsonValueKind.String);
                break;
        }

        // 分派到具体协议类型
        ProtocolDto dto = interfaceType switch
        {
            InterfaceType.LAN => root.Deserialize<LanProtocolDto>(options)!,
            InterfaceType.COM => root.Deserialize<SerialProtocolDto>(options)!,
            InterfaceType.API => root.Deserialize<ApiProtocolDto>(options)!,
            InterfaceType.DATABASE => root.Deserialize<DatabaseProtocolDto>(options)!,
            _ => throw new JsonException($"不支持的接口类型: {interfaceType}")
        };

        return dto;
    }

    private static void CrossObjectValidatePoints(JsonElement root, ProtocolType protocolType)
    {
        var equipments = root.GetProperty(nameof(ProtocolDto.Equipments));

        switch (protocolType)
        {
            case ProtocolType.ModbusTcpNet:
            case ProtocolType.ModbusRtu:
            case ProtocolType.ModbusRtuOverTcp:
                {
                    foreach (var equipment in equipments.EnumerateArray())
                    {
                        if (!equipment.TryGetProperty(nameof(EquipmentDto.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                            throw new JsonException("设备缺少参数列表");

                        foreach (var parameter in parameters.EnumerateArray())
                        {
                            //存在性校验
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.StationNo));
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.DataFormat));
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.DataType));
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.AddressStartWithZero));

                            //字段类型校验
                            JsonValidateHelper.EnsurePropertyTypeIsRight<string>(parameter, nameof(ParameterDto.StationNo), JsonValueKind.String);
                            JsonValidateHelper.EnsureEnumIsRight<DataFormat>(parameter, nameof(ParameterDto.DataFormat));
                            JsonValidateHelper.EnsureEnumIsRight<DataType>(parameter, nameof(ParameterDto.DataType));
                            JsonValidateHelper.EnsurePropertyTypeIsRight<bool>(parameter, nameof(ParameterDto.AddressStartWithZero), JsonValueKind.True);
                        }
                    }
                }
                break;

            case ProtocolType.DLT6452007OverTcp:
            case ProtocolType.DLT6452007Serial:
                {
                    foreach (var equipment in equipments.EnumerateArray())
                    {
                        if (!equipment.TryGetProperty(nameof(EquipmentDto.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                            throw new JsonException("设备缺少参数列表");

                        foreach (var parameter in parameters.EnumerateArray())
                        {
                            //存在性校验
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.StationNo));

                            //字段类型校验
                            JsonValidateHelper.EnsurePropertyTypeIsRight<string>(parameter, nameof(ParameterDto.StationNo), JsonValueKind.String);
                        }
                    }
                }
                break;

            case ProtocolType.CJT1882004OverTcp:
            case ProtocolType.CJT1882004Serial:
                {
                    foreach (var equipment in equipments.EnumerateArray())
                    {
                        if (!equipment.TryGetProperty(nameof(EquipmentDto.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                            throw new JsonException("设备缺少参数列表");

                        foreach (var parameter in parameters.EnumerateArray())
                        {
                            //存在性校验
                            JsonValidateHelper.EnsurePropertyExists(parameter, nameof(ParameterDto.StationNo));

                            //字段类型校验
                            JsonValidateHelper.EnsurePropertyTypeIsRight<string>(parameter, nameof(ParameterDto.StationNo), JsonValueKind.String);
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    public override void Write(Utf8JsonWriter writer, ProtocolDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}