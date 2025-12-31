namespace KEDA_Share.Enums;

public enum ProtocolType
{
    Api,

    ApiWithOnlyOneAddress,

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
    OPCUA,

    S200Smart,

    S7300,

    S71200,

    S71500,

    MySQL,

    MySqlOnlyOneAddress,

    FJ1000Jet,

    FJ60W,

    GP1125T,
}