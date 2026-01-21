using KEDA_CommonV2.Converters.Workstation;
using System.Text.Json;

namespace KEDA_CommonV2.Utilities;
public static class JsonOptionsProvider
{
    public static readonly JsonSerializerOptions WorkstationJsonOptions;

    static JsonOptionsProvider()
    {
        WorkstationJsonOptions = new JsonSerializerOptions()
        {
            AllowTrailingCommas = false // 明确禁止末尾逗号
        }; ;
        WorkstationJsonOptions.Converters.Add(new WorkstationJsonConverter());
        WorkstationJsonOptions.Converters.Add(new ProtocolJsonConverter());
        WorkstationJsonOptions.Converters.Add(new EquipmentJsonConverter());
        WorkstationJsonOptions.Converters.Add(new ParameterJsonConverter());
    }
}