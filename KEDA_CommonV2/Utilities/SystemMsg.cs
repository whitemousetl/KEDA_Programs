using System.Net;
using System.Net.Sockets;

namespace KEDA_CommonV2.Utilities;

public static class SystemMsg
{
    public static string GetLocalIp()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
            ?.ToString() ?? "unknown";
    }
}