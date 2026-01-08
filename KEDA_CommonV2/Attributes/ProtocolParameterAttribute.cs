using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Attributes;
/// <summary>
/// ProtocolParameterAttribute 是一个自定义特性（Attribute），它的作用是给协议类型（ProtocolType 枚举的每个值）“打标签”，标记这个协议在参数校验时需要哪些字段（比如是否必须有站号、数据格式、数据类型等）。
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ProtocolParameterAttribute : Attribute
{
    public bool RequireStationNo { get; set; }
    public bool RequireDataFormat { get; set; }
    public bool RequireDataType { get; set; }
    // 可扩展更多参数
    public ProtocolParameterAttribute(bool requireStationNo = false, bool requireDataFormat = false, bool requireDataType = false)
    {
        RequireStationNo = requireStationNo;
        RequireDataFormat = requireDataFormat;
        RequireDataType = requireDataType;
    }
}
