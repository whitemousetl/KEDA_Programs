using HslCommunication.Profinet.Melsec;
using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Base;

namespace KEDA_ControllerV2.Protocols.Serial;

[SupportedProtocolType(ProtocolType.FxSerial)]
public class FxSerialDriver : SerialBaseProtocolDriver<MelsecFxSerial>
{
    protected override MelsecFxSerial CreateConnection(ProtocolDto protocol, CancellationToken token)
    {
        if (protocol is SerialProtocolDto serialProtocol)
        {
            var conn = new MelsecFxSerial();
            conn.SerialPortInni(serialProtocol.SerialPortName, (int)serialProtocol.BaudRate, (int)serialProtocol.DataBits, serialProtocol.StopBits, serialProtocol.Parity);
            conn.ReceiveTimeOut = serialProtocol.ReceiveTimeOut;
            return conn;
        }
        else
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
    }
}