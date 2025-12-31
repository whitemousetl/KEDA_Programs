namespace KEDA_CommonV2.Enums;

public enum ProtocolType
{
    #region LAN
    ModbusTcpNet,

    ModbusRtuOverTcp,

    OmronFinsNet,

    OmronCipNet,

    SiemensS200Smart,

    SiemensS1200,

    SiemensS1500,

    SiemensS200,

    SiemensS300,

    SiemensS400,

    DLT6452007OverTcp,

    CJT1882004OverTcp,

    FxSerialOverTcp,

    IEC104,

    OpcUa,

    #region Udp
    OmronFinsUdp,
    #endregion

    #region 自由协议
    FJ1000Jet,

    FJ60W,

    GP1125T,

    BottomImageF1970,
    #endregion

    #endregion

    #region COM
    ModbusRtu,

    DLT6452007Serial,

    CJT1882004Serial,

    FxSerial,
    #endregion

    #region API
    Api,
    #endregion

    #region DATABASE
    MySQL, 
    #endregion
}