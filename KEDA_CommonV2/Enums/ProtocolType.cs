using KEDA_CommonV2.Attributes;
using System.ComponentModel;

namespace KEDA_CommonV2.Enums;

/// <summary>
/// 协议类型
/// </summary>
public enum ProtocolType
{
    #region LAN
    /// <summary>
    /// ModbusTcpNet
    /// </summary>
    [Description("ModbusTcpNet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    ModbusTcpNet = 0,

    /// <summary>
    /// ModbusRtuOverTcp
    /// </summary>
    [Description("ModbusRtuOverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    ModbusRtuOverTcp = 1,

    /// <summary>
    /// OmronFinsNet
    /// </summary>
    [Description("OmronFinsNet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    OmronFinsNet = 2,

    /// <summary>
    /// OmronCipNet
    /// </summary>
    [Description("OmronCipNet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    OmronCipNet = 3,

    /// <summary>
    /// SiemensS200Smart
    /// </summary>
    [Description("SiemensS200Smart")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS200Smart = 4,

    /// <summary>
    /// SiemensS1200
    /// </summary>
    [Description("SiemensS1200")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS1200 = 5,

    /// <summary>
    /// SiemensS1500
    /// </summary>
    [Description("SiemensS1500")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS1500 = 6,

    /// <summary>
    /// SiemensS200
    /// </summary>
    [Description("SiemensS200")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS200 = 7,

    /// <summary>
    /// SiemensS300
    /// </summary>
    [Description("SiemensS300")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS300 = 8,

    /// <summary>
    /// SiemensS400
    /// </summary>
    [Description("SiemensS400")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    SiemensS400 = 9,

    /// <summary>
    /// DLT6452007OverTcp
    /// </summary>
    [Description("DLT6452007OverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    DLT6452007OverTcp = 10,

    /// <summary>
    /// CJT1882004OverTcp
    /// </summary>
    [Description("CJT1882004OverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    CJT1882004OverTcp = 11,

    /// <summary>
    /// FxSerialOverTcp
    /// </summary>
    [Description("FxSerialOverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    FxSerialOverTcp = 12,

    /// <summary>
    /// IEC104
    /// </summary>
    [Description("IEC104")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    IEC104 = 13,

    /// <summary>
    /// OpcUa
    /// </summary>
    [Description("OpcUa")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    OpcUa = 14,

    #region Udp
    /// <summary>
    /// OmronFinsUdp
    /// </summary>
    [Description("OmronFinsUdp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    OmronFinsUdp = 15,
    #endregion

    #region 自由协议
    /// <summary>
    /// FJ1000Jet
    /// </summary>
    [Description("FJ1000Jet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    FJ1000Jet = 16,

    /// <summary>
    /// FJ60W
    /// </summary>
    [Description("FJ60W")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    FJ60W = 17,

    /// <summary>
    /// GP1125T
    /// </summary>
    [Description("GP1125T")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    GP1125T = 18,

    /// <summary>
    /// BottomImageF1970
    /// </summary>
    [Description("BottomImageF1970")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    BottomImageF1970 = 19,
    #endregion

    #endregion

    #region COM
    /// <summary>
    /// ModbusRtu
    /// </summary>
    [Description("ModbusRtu")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    ModbusRtu = 100,

    /// <summary>
    /// DLT6452007Serial
    /// </summary>
    [Description("DLT6452007Serial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    DLT6452007Serial = 101,

    /// <summary>
    /// CJT1882004Serial
    /// </summary>
    [Description("CJT1882004Serial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    CJT1882004Serial = 102,

    /// <summary>
    /// FxSerial
    /// </summary>
    [Description("FxSerial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    FxSerial = 103,
    #endregion

    #region API
    /// <summary>
    /// Api
    /// </summary>
    [Description("Api")]
    [ProtocolInterfaceType(InterfaceType.API)]
    Api = 200,
    #endregion

    #region DATABASE
    /// <summary>
    /// MySQL
    /// </summary>
    [Description("MySQL")]
    [ProtocolInterfaceType(InterfaceType.DATABASE)]
    MySQL = 300,
    #endregion
}