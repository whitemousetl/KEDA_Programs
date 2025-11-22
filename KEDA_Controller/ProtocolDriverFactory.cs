using KEDA_Common.Enums;
using KEDA_Controller.Interfaces;

namespace KEDA_Controller;
public static class ProtocolDriverFactory
{
    private static readonly Dictionary<ProtocolType, Type> _typeMap;

    static ProtocolDriverFactory()
    {
        var protocolNamespace = "KEDA_Controller.Protocols";

        _typeMap = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.Namespace != null &&
                t.Namespace.Equals(protocolNamespace, StringComparison.OrdinalIgnoreCase) &&
                typeof(IProtocolDriver).IsAssignableFrom(t) &&
                !t.IsAbstract)
            .SelectMany(t => t.GetCustomAttributes(typeof(ProtocolTypeAttribute), false)
            .Cast<ProtocolTypeAttribute>()
            .Select(attr => new { attr.ProtocolType, Type = t }))
            .ToDictionary(x => x.ProtocolType, x => x.Type);
    }

    public static IProtocolDriver? CreateDriver(ProtocolType protocolType)
    {
        try
        {
            if (_typeMap.TryGetValue(protocolType, out var type))
                return Activator.CreateInstance(type) as IProtocolDriver;
            return null;
        }
        catch
        {
            return null;
        }
    }
}
