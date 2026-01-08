using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KEDA_CommonV2.Utilities;
public static class ProtocolTypeHelper
{
    private static readonly Dictionary<InterfaceType, HashSet<ProtocolType>> _interfaceToProtocolTypes;

    static ProtocolTypeHelper()
    {
        _interfaceToProtocolTypes = [];
        var type = typeof(ProtocolType);
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (field.GetCustomAttributes(typeof(ProtocolInterfaceTypeAttribute), false)
                            .FirstOrDefault() is ProtocolInterfaceTypeAttribute attr)
            {
                var protocolType = (ProtocolType)field.GetValue(null)!;
                if (!_interfaceToProtocolTypes.TryGetValue(attr.InterfaceType, out var set))
                {
                    set = [];
                    _interfaceToProtocolTypes[attr.InterfaceType] = set;
                }
                set.Add(protocolType);
            }
        }
    }

    public static bool IsProtocolTypeValidForInterface(InterfaceType interfaceType, ProtocolType protocolType)
    {
        return _interfaceToProtocolTypes.TryGetValue(interfaceType, out var set) && set.Contains(protocolType);
    }
}
