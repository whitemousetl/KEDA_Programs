using KEDA_CommonV2.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Utilities;
public static class JsonOptionsProvider
{
    public static readonly JsonSerializerOptions ProtocolJsonOptions;

    static JsonOptionsProvider()
    {
        ProtocolJsonOptions = new JsonSerializerOptions()
        {
            AllowTrailingCommas = false // 明确禁止末尾逗号
        }; ;
        ProtocolJsonOptions.Converters.Add(new ProtocolJsonConverter());
        // 可根据需要添加更多全局设置
        // WorkstationOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}