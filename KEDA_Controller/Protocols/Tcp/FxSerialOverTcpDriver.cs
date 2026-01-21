using HslCommunication.Profinet.Melsec;
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
[ProtocolType(ProtocolType.FxSerialOverTcp)]
public class FxSerialOverTcpDriver : HslTcpBaseProtocolDriver<MelsecFxSerialOverTcp>
{
    public FxSerialOverTcpDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override MelsecFxSerialOverTcp CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
        };
    }
}
