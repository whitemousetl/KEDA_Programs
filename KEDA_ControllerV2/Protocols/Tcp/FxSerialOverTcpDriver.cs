using HslCommunication.Profinet.Melsec;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_Controller.Base;

namespace KEDA_ControllerV2.Protocols.Tcp;

[ProtocolType(ProtocolType.FxSerialOverTcp)]
public class FxSerialOverTcpDriver : TcpBaseProtocolDriver<MelsecFxSerialOverTcp>
{
    protected override MelsecFxSerialOverTcp CreateConnection(ProtocolDto protocol, CancellationToken token)
    {
        if (protocol is LanProtocolDto lanProtocol)
        {
            var conn = new MelsecFxSerialOverTcp()
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