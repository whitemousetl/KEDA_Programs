using HslCommunication.ModBus;
using KEDA_Common.Enums;
using KEDA_Common.Interfaces;
using KEDA_Common.Model;
using KEDA_Controller.Base;

namespace KEDA_Controller.Protocols.Tcp;
[ProtocolType(ProtocolType.Modbus)]
[ProtocolType(ProtocolType.ModbusTcp)]
public class ModbusDriver : HslTcpBaseProtocolDriver<ModbusTcpNet>
{
    public ModbusDriver(IMqttPublishService mqttPublishService) : base(mqttPublishService)
    {
    }

    protected override ModbusTcpNet CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        return new(protocol.IPAddress, protocol.ProtocolPort)
        {
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
            AddressStartWithZero = protocol.AddressStartWithZero,
        };
    }
}
