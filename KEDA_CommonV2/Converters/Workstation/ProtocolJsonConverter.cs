using HslCommunication.Core;
using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_CommonV2.Utilities;
using System.Drawing;
using System.IO.Ports;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace KEDA_CommonV2.Converters.Workstation;

public class ProtocolJsonConverter : JsonConverter<ProtocolDto>
{
    public override ProtocolDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var namePrefix = "Protocol的";

        //Id
        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ProtocolDto.Id),  JsonValueKind.String);
        //interfaceType
        var interfaceType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<InterfaceType>(root, namePrefix, nameof(ProtocolDto.InterfaceType));
        //protocolType
        var protocolType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<ProtocolType>(root, namePrefix, nameof(ProtocolDto.ProtocolType));

        namePrefix = protocolType.ToString() + "的";

        //字段间关系，根据接口类型限制协议类型，比如接口类型是LAN时，如果协议类型是ModbusRtu，则不被支持
        if (!ProtocolTypeHelper.IsProtocolTypeValidForInterface(interfaceType, protocolType))
            throw new JsonException($"接口类型{interfaceType}下不支持协议类型{protocolType}");

        //equipments
        var equipments = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<EquipmentDto>>(root, namePrefix, nameof(ProtocolDto.Equipments), JsonValueKind.Array);

        //非必须存在字段，如果存在，校验其类型
        JsonValidateHelper.ValidateOptionalFields(
            root, 
            namePrefix, 
            (nameof(ProtocolDto.CollectCycle), JsonValueKind.Number),
            (nameof(ProtocolDto.ReceiveTimeOut), JsonValueKind.Number),
            (nameof(ProtocolDto.ConnectTimeOut), JsonValueKind.Number),
            (nameof(ProtocolDto.Account), JsonValueKind.String),
            (nameof(ProtocolDto.Password), JsonValueKind.String),
            (nameof(ProtocolDto.Remark), JsonValueKind.String),
            (nameof(ProtocolDto.AdditionalOptions), JsonValueKind.String));

        // 跨对象校验：直接遍历JSON
        CrossObjectValidatePoints(root, protocolType, namePrefix);

        // 子类特有字段存在性校验和字段类型校验
        switch (interfaceType)
        {
            case InterfaceType.LAN:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(LanProtocolDto.IpAddress), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<int>(root, namePrefix, nameof(LanProtocolDto.ProtocolPort), JsonValueKind.Number);
                JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(LanProtocolDto.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.COM:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(SerialProtocolDto.SerialPortName), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<BaudRateType>(root, namePrefix, nameof(SerialProtocolDto.BaudRate));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataBitsType>(root, namePrefix, nameof(SerialProtocolDto.DataBits));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<Parity>(root, namePrefix, nameof(SerialProtocolDto.Parity));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<StopBits>(root, namePrefix, nameof(SerialProtocolDto.StopBits));
                break;
            case InterfaceType.API:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ApiProtocolDto.AccessApiString), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<RequestMethod>(root, namePrefix, nameof(ApiProtocolDto.RequestMethod));
                JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(ApiProtocolDto.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.DATABASE:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(DatabaseProtocolDto.QuerySqlString), JsonValueKind.String);
                JsonValidateHelper.ValidateOptionalFields(
                    root, 
                    namePrefix, 
                    (nameof(DatabaseProtocolDto.Gateway), JsonValueKind.String),
                    (nameof(DatabaseProtocolDto.IpAddress), JsonValueKind.String),
                    (nameof(DatabaseProtocolDto.DatabaseName), JsonValueKind.String),
                    (nameof(DatabaseProtocolDto.DatabaseConnectString), JsonValueKind.String));
                JsonValidateHelper.ValidateOptionalFields<int?>(root, namePrefix, nameof(DatabaseProtocolDto.ProtocolPort), JsonValueKind.Number);
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

    private static void CrossObjectValidatePoints(JsonElement root, ProtocolType protocolType, string namePrefix)
    {
        var equipments = root.GetProperty(nameof(ProtocolDto.Equipments));

        // 获取枚举字段上的特性
        var fieldInfo = typeof(ProtocolType).GetField(protocolType.ToString());
        var attr = fieldInfo?.GetCustomAttribute<ProtocolValidateParameterAttribute>();

        if (attr == null) return;

        foreach (var equipment in equipments.EnumerateArray())
        {
            if (!equipment.TryGetProperty(nameof(EquipmentDto.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                throw new JsonException("设备缺少参数列表");

            foreach (var parameter in parameters.EnumerateArray())
            {
                // 站号校验
                if (attr.RequireStationNo)
                    JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(parameter, namePrefix, nameof(ParameterDto.StationNo),  JsonValueKind.String);
                // 数据格式校验
                if (attr.RequireDataFormat)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataFormat>(parameter, namePrefix, nameof(ParameterDto.DataFormat));
                // 数据类型校验
                if (attr.RequireDataType)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataType>(parameter, namePrefix, nameof(ParameterDto.DataType));
                // 地址从0开始校验
                if (attr.RequireAddressStartWithZero)
                    JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(parameter, namePrefix, nameof(ParameterDto.AddressStartWithZero), JsonValueKind.True);
                // 仪表类型校验
                if (attr.RequireInstrumentType)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<InstrumentType>(parameter, namePrefix, nameof(ParameterDto.InstrumentType));
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, ProtocolDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}