using HslCommunication.Instrument.CJT;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_Controller.Base;

namespace KEDA_ControllerV2.Protocols.Tcp;

[ProtocolType(ProtocolType.CJT1882004OverTcp)]
public class CJT188OverTcp2004Driver : TcpBaseProtocolDriver<CJT188OverTcp>
{
    protected override CJT188OverTcp CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is LanProtocol lanProtocol)
        {
            var conn = new CJT188OverTcp("1")
            {
                IpAddress = lanProtocol.IpAddress,
                Port = lanProtocol.ProtocolPort,
                ReceiveTimeOut = lanProtocol.ReceiveTimeOut,
                ConnectTimeOut = lanProtocol.ConnectTimeOut,
            };
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 LanProtocol，无法进行操作。");
    }
}