using HslCommunication.Instrument.DLT;
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
[ProtocolType(ProtocolType.DLT6452007OverTcp)]
[ProtocolType(ProtocolType.DLT645_2007OverTcp)]
public class DLT6452007OverTcpDriver : HslTcpBaseProtocolDriver<DLT645OverTcp>
{
    public DLT6452007OverTcpDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }
    protected override DLT645OverTcp CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}
