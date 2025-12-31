using HslCommunication.Profinet.Siemens;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_Controller.Base;

namespace KEDA_ControllerV2.Protocols.Tcp;

[ProtocolType(ProtocolType.SiemensS200Smart)]
public class SiemensS200SmartDriver : TcpBaseProtocolDriver<SiemensS7Net>
{
    protected override SiemensS7Net CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is LanProtocol lanProtocol)
        {
            var conn = new SiemensS7Net(SiemensPLCS.S200Smart, lanProtocol.IpAddress)
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