using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SupportedProtocolTypeAttribute : Attribute
{
    public ProtocolType ProtocolType { get; }

    public SupportedProtocolTypeAttribute(ProtocolType type)
    {
        ProtocolType = type;
    }
}