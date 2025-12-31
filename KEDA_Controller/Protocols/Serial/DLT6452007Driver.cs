using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.DLT;
using KEDA_Common.Enums;
using KEDA_Common.Model;
using KEDA_Controller.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Controller.Protocols.Serial;
[ProtocolType(ProtocolType.DLT645)]
[ProtocolType(ProtocolType.DLT6452007)]
[ProtocolType(ProtocolType.DLT6452007Serial)]
[ProtocolType(ProtocolType.DLT645_2007)]
[ProtocolType(ProtocolType.DLT645_2007Serial)]
public class DLT6452007Driver : HslSerialBaseProtocolDriver<DLT645>
{
    protected override DLT645 CreateConnection(WorkstationEntity protocol, CancellationToken token)
    {
        var conn = new DLT645();
        conn.SerialPortInni(protocol.PortName, protocol.BaudRate, protocol.DataBits, protocol.StopBits, protocol.Parity);
        conn.ReceiveTimeOut = protocol.ReceiveTimeOut;
        return conn;
    }
}
