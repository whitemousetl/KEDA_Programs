using HslCommunication.ModBus;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_Controller.Base;

namespace KEDA_ControllerV2.Protocols.Tcp;

[ProtocolType(ProtocolType.ModbusRtuOverTcp)]
public class ModbusRtuOverTcpDriver : TcpBaseProtocolDriver<ModbusRtuOverTcp>
{
    protected override ModbusRtuOverTcp CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is LanProtocol lanProtocol)
        {
            var conn = new ModbusRtuOverTcp()
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