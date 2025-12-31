using HslCommunication.Profinet.Melsec;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_ControllerV2.Base;

namespace KEDA_ControllerV2.Protocols.Serial;

[ProtocolType(ProtocolType.FxSerial)]
public class FxSerialDriver : SerialBaseProtocolDriver<MelsecFxSerial>
{
    protected override MelsecFxSerial CreateConnection(Protocol protocol, CancellationToken token)
    {
        if (protocol is SerialProtocol serialProtocol)
        {
            var conn = new MelsecFxSerial();
            conn.SerialPortInni(serialProtocol.PortName, serialProtocol.BaudRate, serialProtocol.DataBits, serialProtocol.StopBits, serialProtocol.Parity);
            conn.ReceiveTimeOut = serialProtocol.ReceiveTimeOut;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
    }
}