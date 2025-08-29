using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Share.Enums;
public enum ProtocolType
{
    Modbus,
    ModbusTcp,
    ModbusRtu,
    ModbusRtuOverTcp,
    DLT6452007OverTcp,
    DLT6452007Serial,
    Fins,
    FinsTcp,
    FinsUdp,
    CipNet,
    FinsCipNet,
    CJT188OverTcp_2004,
    FxSerialOverTcp,
    IEC104,
    OPC,
    S200Smart,
    S7300,
    S71200,
    S71500,
    MySQL,
    FJ1000Jet,
    FJ60W,
    MySql,
    GP1125T,
}
