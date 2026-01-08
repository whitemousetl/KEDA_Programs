using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Attributes;
/// <summary>
/// 这个特性是用来给某个类“打标签”，标记这个类支持哪些协议类型（ProtocolType）。比如你有一个类叫 FinsTcpDriver，它支持 OmronFinsNet 协议。就在FinsTcpDriver打上[SupportedProtocolType(ProtocolType.OmronFinsNet)]标签
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SupportedProtocolTypeAttribute : Attribute
{
    public ProtocolType ProtocolType { get; }

    public SupportedProtocolTypeAttribute(ProtocolType type)
    {
        ProtocolType = type;
    }
}