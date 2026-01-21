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
[ProtocolType(ProtocolType.S7400)]
[ProtocolType(ProtocolType.S400)]
public class SiemensS400Driver : HslTcpBaseProtocolDriver<SiemensS7Net>
{
    public SiemensS400Driver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override SiemensS7Net CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(SiemensPLCS.S400, protocol.IPAddress)
        {
            Port = protocol.ProtocolPort,
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}