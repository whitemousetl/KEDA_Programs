using HslCommunication.Instrument.DLT;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Base;

namespace KEDA_ControllerV2.Protocols.Serial;

[ProtocolType(ProtocolType.DLT6452007Serial)]
public class DLT6452007Driver : SerialBaseProtocolDriver<DLT645>
{
    protected override DLT645 CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is SerialProtocol serialProtocol)
        {
            var conn = new DLT645();
            conn.SerialPortInni(serialProtocol.PortName, serialProtocol.BaudRate, serialProtocol.DataBits, serialProtocol.StopBits, serialProtocol.Parity);
            conn.ReceiveTimeOut = serialProtocol.ReceiveTimeOut;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
    }
}