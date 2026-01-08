using KEDA_CommonV2.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_CommonV2.Attributes;
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ProtocolInterfaceTypeAttribute : Attribute
{
    public InterfaceType InterfaceType { get; }
    public ProtocolInterfaceTypeAttribute(InterfaceType interfaceType)
    {
        InterfaceType = interfaceType;
    }
}
