using HslCommunication.Instrument.CJT;
using KEDA_CommonV2.Attributes;
using KEDA_CommonV2.Enums;
using KEDA_CommonV2.Model.Workstations.Protocols;
using KEDA_ControllerV2.Base;

namespace KEDA_ControllerV2.Protocols.Serial;

[SupportedProtocolType(ProtocolType.CJT1882004Serial)]
public class CJT188Driver : SerialBaseProtocolDriver<CJT188>
{
    protected override CJT188 CreateConnection(ProtocolDto protocol, CancellationToken token)
    {
        if (protocol is SerialProtocolDto serialProtocol)
        {
            if (!serialProtocol.StopBits.HasValue)
                throw new InvalidOperationException($"{_protocolName}协议未指定 StopBits");
            if (!serialProtocol.Parity.HasValue)
                throw new InvalidOperationException($"{_protocolName}协议未指定 Parity");

            if (!serialProtocol.BaudRate.HasValue)
                throw new InvalidOperationException($"{_protocolName}协议未指定 BaudRate");
            if (!serialProtocol.DataBits.HasValue)
                throw new InvalidOperationException($"{_protocolName}协议未指定 DataBits");

            var conn = new CJT188("1");

            conn.SerialPortInni(
                serialProtocol.SerialPortName,
                (int)serialProtocol.BaudRate.Value,
                (int)serialProtocol.DataBits.Value,
                serialProtocol.StopBits.Value,
                serialProtocol.Parity.Value
            );
            conn.ReceiveTimeOut = serialProtocol.ReceiveTimeOut;
            return conn;
        }
        else
        {
            throw new InvalidOperationException($"{_protocolName}协议类型不是 SerialProtocol，无法进行操作。");
        }
    }
}