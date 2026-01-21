using HslCommunication.Profinet.Omron;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Udp;
[ProtocolType(ProtocolType.FinsUdp)]
public class FinsUdpProtocolDriver : HslUdpBaseProtocolDriver<OmronFinsUdp>
{
    protected override OmronFinsUdp CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        var conn =  new OmronFinsUdp()
        {
            CommunicationPipe = new HslCommunication.Core.Pipe.PipeUdpNet(protocol.IPAddress, protocol.ProtocolPort)
            {
                ReceiveTimeOut = protocol.ReceiveTimeOut,    // 接收设备数据反馈的超时时间
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
}
