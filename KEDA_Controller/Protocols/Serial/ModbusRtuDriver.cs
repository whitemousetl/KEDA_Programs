using HslCommunication.ModBus;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Serial;
[ProtocolType(ProtocolType.ModbusRtu)]
[ProtocolType(ProtocolType.ModbusRtuSerial)]
public class ModbusRtuDriver : HslSerialBaseProtocolDriver<ModbusRtu>
{
    protected override ModbusRtu CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        var conn = new ModbusRtu();
        conn.SerialPortInni(protocol.PortName, protocol.BaudRate, protocol.DataBits, protocol.StopBits, protocol.Parity);
        conn.AddressStartWithZero = protocol.AddressStartWithZero;
        conn.ReceiveTimeOut = protocol.ReceiveTimeOut;
        return conn;
    }
}
