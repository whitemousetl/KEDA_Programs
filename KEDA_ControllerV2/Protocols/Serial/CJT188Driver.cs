using HslCommunication.Instrument.CJT;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Base;

namespace KEDA_ControllerV2.Protocols.Serial;

[ProtocolType(ProtocolType.CJT1882004Serial)]
public class CJT188Driver : SerialBaseProtocolDriver<CJT188>
{
    protected override CJT188 CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is SerialProtocol serialProtocol)
        {
            var conn = new CJT188("1");
            conn.SerialPortInni(serialProtocol.PortName, serialProtocol.BaudRate, serialProtocol.DataBits, serialProtocol.StopBits, serialProtocol.Parity);
            conn.ReceiveTimeOut = serialProtocol.ReceiveTimeOut;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
    }
}