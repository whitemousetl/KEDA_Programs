using HslCommunication.Profinet.Siemens;
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
[ProtocolType(ProtocolType.S7200)]
[ProtocolType(ProtocolType.S200)]
public class SiemensS200Driver : HslTcpBaseProtocolDriver<SiemensS7Net>
{
    public SiemensS200Driver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override SiemensS7Net CreateConnection(WorkstationEntity protocol, CancellationToken token)
    {
        return new(SiemensPLCS.S200, protocol.IPAddress)
        {
            Port = protocol.ProtocolPort,
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}
