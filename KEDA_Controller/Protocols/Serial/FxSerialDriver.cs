using HslCommunication.Instrument.DLT;
using HslCommunication.Profinet.Melsec;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Serial;
[ProtocolType(ProtocolType.FxSerial)]
public class FxSerialDriver : HslSerialBaseProtocolDriver<MelsecFxSerial>
{
    protected override MelsecFxSerial CreateConnection(ProtocolEntity protocol, CancellationToken token)
    {
        var conn = new MelsecFxSerial();
        conn.SerialPortInni(protocol.PortName, protocol.BaudRate, protocol.DataBits, protocol.StopBits, protocol.Parity);
        conn.ReceiveTimeOut = protocol.ReceiveTimeOut;
        return conn;
    }
}

