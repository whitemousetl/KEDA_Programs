using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEDA_Common.Enums;
public enum ProtocolType
{
    //Tcp
    Modbus,
    ModbusTcp,

    Fins,
    FinsTcp,

    CipNet,
    FinsCipNet,

    IEC104,

    OPC,
    OPCUA,

    S200Smart,

    S71200,
    S1200,

    S71500,
    S1500,

    S7200,
    S200,

    S7300,
    S300,

    S7400,
    S400,

    MySQL,

    FJ1000Jet,

    FJ60W,

    GP1125T,

    BottomImageF1970,

    //Serial or OverTcp
    ModbusRtu,
    ModbusRtuSerial,

    ModbusRtuOverTcp,

    DLT645,
    DLT6452007,
    DLT6452007Serial,
    DLT645_2007,
    DLT645_2007Serial,

    DLT645_2007OverTcp,
    DLT6452007OverTcp,

    CJT188,
    CJT188Serial,
    CJT188_2004,
    CJT188_2004Serial,

    CJT188OverTcp_2004,
    CJT188OverTcp,
    CJT188OverTcp2004,

    FxSerial,

    FxSerialOverTcp,

    //Udp
    FinsUdp,
}