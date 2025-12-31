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

            ProtocolType.ModbusRtuOverTcp => new ModbusRtuOverTcpDriver(),

            ProtocolType.ModbusRtu => new ModbusRtuDriver(),

            ProtocolType.Fins => new FinsTcpProtocolDriver(),
            ProtocolType.FinsTcp => new FinsTcpProtocolDriver(),

            ProtocolType.FinsUdp => new FinsUdpProtocolDriver(),

            ProtocolType.CipNet => new FinsCipNetProtocolDriver(),
            ProtocolType.FinsCipNet => new FinsCipNetProtocolDriver(),

            ProtocolType.S71500 => new SiemensS71500Driver(),

            ProtocolType.S200Smart => new SiemensSmart200Driver(),

            ProtocolType.S71200 => new SiemensS71200Driver(),

            ProtocolType.DLT6452007OverTcp => new DLT645_2007OverTcpDriver(),
            ProtocolType.DLT6452007Serial => new DLT645_2007SerialDriver(),

            ProtocolType.MySQL => new MySQLDevier(),
            ProtocolType.MySqlOnlyOneAddress => new MySqlOnlyOneAddressDriver(),

            ProtocolType.OPC => new OpcUaDriver(),
            ProtocolType.OPCUA => new OpcUaDriver(),

            ProtocolType.Api => new ApiDriver(),
            ProtocolType.ApiWithOnlyOneAddress => new ApiWithOnlyOneAddressDriver(),

            _ => null
        };
    }
}
