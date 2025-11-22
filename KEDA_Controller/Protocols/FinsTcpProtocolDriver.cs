using HslCommunication.Profinet.Omron;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols;
[ProtocolType(ProtocolType.FinsTcp)]
[ProtocolType(ProtocolType.Fins)]
public class FinsTcpProtocolDriver : HslTcpBaseProtocolDriver<OmronFinsNet>
{
    protected override OmronFinsNet CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}
