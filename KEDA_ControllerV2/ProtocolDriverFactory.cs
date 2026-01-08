using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Interfaces;
using KEDA_ControllerV2.Interfaces;

namespace KEDA_ControllerV2;

public static class ProtocolDriverFactory
{
    private static readonly Dictionary<ProtocolType, Type> _typeMap;

    static ProtocolDriverFactory()
    {
        var protocolNamespace = "KEDA_ControllerV2.Protocols";

        _typeMap = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.Namespace != null &&
                t.Namespace.StartsWith(protocolNamespace, StringComparison.OrdinalIgnoreCase) &&
                typeof(IProtocolDriver).IsAssignableFrom(t) &&
                !t.IsAbstract)
            .SelectMany(t => t.GetCustomAttributes(typeof(SupportedProtocolTypeAttribute), false)
            .Cast<SupportedProtocolTypeAttribute>()
            .Select(attr => new { attr.ProtocolType, Type = t }))
            .ToDictionary(x => x.ProtocolType, x => x.Type);
    }

    public static IProtocolDriver? CreateDriver(ProtocolType protocolType, IMqttPublishService? mqttPublishService = null)
    {
        try
        {
            if (_typeMap.TryGetValue(protocolType, out var type))
            {
                //查找构造函数
                var ctor = type.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .FirstOrDefault();

                if (ctor == null) return null;

                var parameters = ctor.GetParameters();
                if (parameters.Length == 0)
                    return Activator.CreateInstance(type) as IProtocolDriver;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}