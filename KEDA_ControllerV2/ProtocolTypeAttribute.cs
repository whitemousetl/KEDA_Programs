using KEDA_CommonV2.Enums;

namespace KEDA_ControllerV2;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProtocolTypeAttribute : Attribute
{
    public ProtocolType ProtocolType { get; }

    public ProtocolTypeAttribute(ProtocolType type)
    {
        ProtocolType = type;
    }
}