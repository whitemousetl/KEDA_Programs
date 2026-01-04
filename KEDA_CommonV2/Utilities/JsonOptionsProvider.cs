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
    public static readonly JsonSerializerOptions WorkstationOptions;

    static JsonOptionsProvider()
    {
        WorkstationOptions = new JsonSerializerOptions();
        WorkstationOptions.Converters.Add(new ProtocolJsonConverter());
        // 可根据需要添加更多全局设置
        // WorkstationOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}