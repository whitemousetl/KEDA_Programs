using HslCommunication.Profinet.Omron;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_Controller.Base;

namespace KEDA_ControllerV2.Protocols.Tcp.Udp;

[ProtocolType(ProtocolType.OmronFinsUdp)]
public class FinsUdpProtocolDriver : UdpBaseProtocolDriver<OmronFinsUdp>
{
    protected override OmronFinsUdp CreateConnection(ProtocolDto protocol, CancellationToken token)
    {
        if (protocol is LanProtocolDto lanProtocol)
        {
            var conn = new OmronFinsUdp()
            {
                CommunicationPipe = new HslCommunication.Core.Pipe.PipeUdpNet(lanProtocol.IpAddress, lanProtocol.ProtocolPort)
                {
                    ReceiveTimeOut = lanProtocol.ReceiveTimeOut,    // 接收设备数据反馈的超时时间
                    SleepTime = 0,
                    SocketKeepAliveTime = -1,
                    IsPersistentConnection = true,
                },
                PlcType = OmronPlcType.CSCJ,
                SA1 = 1,
                GCT = 2,
                DA1 = 0
            };

            conn.ByteTransform.DataFormat = HslCommunication.Core.DataFormat.CDAB;
            conn.ByteTransform.IsStringReverseByteWord = true;

            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 LanProtocol，无法进行操作。");
    }
}