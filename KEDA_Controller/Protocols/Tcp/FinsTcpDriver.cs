using HslCommunication.Profinet.Omron;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Tcp;
[ProtocolType(ProtocolType.FinsTcp)]
[ProtocolType(ProtocolType.Fins)]
public class FinsTcpDriver : HslTcpBaseProtocolDriver<OmronFinsNet>
{
    public FinsTcpDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override OmronFinsNet CreateConnection(WorkstationEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}
