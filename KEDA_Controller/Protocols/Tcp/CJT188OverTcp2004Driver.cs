using HslCommunication.Instrument.CJT;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Base;

namespace KEDA_Controller.Protocols.Tcp;
[ProtocolType(ProtocolType.CJT188OverTcp)]
[ProtocolType(ProtocolType.CJT188OverTcp2004)]
[ProtocolType(ProtocolType.CJT188OverTcp_2004)]
public class CJT188OverTcp2004Driver : HslTcpBaseProtocolDriver<CJT188OverTcp>
{
    public CJT188OverTcp2004Driver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }
    protected override CJT188OverTcp CreateConnection(WorkstationEntity protocol, CancellationToken token)
    {
        return new("1")
        {
            IpAddress = protocol.IPAddress,
            Port = protocol.ProtocolPort,
            InstrumentType = protocol.InstrumentType,
        };
    }
}
