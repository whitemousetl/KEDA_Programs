using KEDA_Share.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorService.Protocols;
public static class ProtocolDriverFactory
{
    public static IProtocolDriver? Create(ProtocolType protocolType)
    {
        return protocolType switch
        {
            ProtocolType.Modbus => new ModbusProtocolDriver(),
            ProtocolType.ModbusTcp => new ModbusProtocolDriver(),

            ProtocolType.Fins => new FinsTcpProtocolDriver(),
            ProtocolType.FinsTcp => new FinsTcpProtocolDriver(),
            ProtocolType.FinsUdp => new FinsUdpProtocolDriver(),

            ProtocolType.S71500 => new SiemensS71500Driver(),
            ProtocolType.S200Smart => new SiemensSmart200Driver(),

            ProtocolType.ModbusRtuOverTcp => new ModbusRtuOverTcpDriver(),

            ProtocolType.ModbusRtu => new ModbusRtuDriver(),

            _ => null
        };
    }
}
