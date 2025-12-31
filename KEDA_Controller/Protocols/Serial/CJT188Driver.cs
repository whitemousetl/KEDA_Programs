using HslCommunication.Instrument.CJT;
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
[ProtocolType(ProtocolType.CJT188)]
[ProtocolType(ProtocolType.CJT188Serial)]
public class CJT188Driver : HslSerialBaseProtocolDriver<CJT188>
{
    protected override CJT188 CreateConnection(WorkstationEntity protocol, CancellationToken token)
    {
        var conn = new CJT188("1");
        conn.SerialPortInni(protocol.PortName, protocol.BaudRate, protocol.DataBits, protocol.StopBits, protocol.Parity);
        conn.ReceiveTimeOut = protocol.ReceiveTimeOut;
        return conn;
    }
}