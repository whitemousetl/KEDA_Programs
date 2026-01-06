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
    ModbusTcpNet = 0,

    /// <summary>
    /// ModbusRtuOverTcp
    /// </summary>
    [Description("ModbusRtuOverTcp")]
    ModbusRtuOverTcp = 1,

    /// <summary>
    /// OmronFinsNet
    /// </summary>
    [Description("OmronFinsNet")]
    OmronFinsNet = 2,

    /// <summary>
    /// OmronCipNet
    /// </summary>
    [Description("OmronCipNet")]
    OmronCipNet = 3,

    /// <summary>
    /// SiemensS200Smart
    /// </summary>
    [Description("SiemensS200Smart")]
    SiemensS200Smart = 4,

    /// <summary>
    /// SiemensS1200
    /// </summary>
    [Description("SiemensS1200")]
    SiemensS1200 = 5,

    /// <summary>
    /// SiemensS1500
    /// </summary>
    [Description("SiemensS1500")]
    SiemensS1500 = 6,

    /// <summary>
    /// SiemensS200
    /// </summary>
    [Description("SiemensS200")]
    SiemensS200 = 7,

    /// <summary>
    /// SiemensS300
    /// </summary>
    [Description("SiemensS300")]
    SiemensS300 = 8,

    /// <summary>
    /// SiemensS400
    /// </summary>
    [Description("SiemensS400")]
    SiemensS400 = 9,

    /// <summary>
    /// DLT6452007OverTcp
    /// </summary>
    [Description("DLT6452007OverTcp")]
    DLT6452007OverTcp = 10,

    /// <summary>
    /// CJT1882004OverTcp
    /// </summary>
    [Description("CJT1882004OverTcp")]
    CJT1882004OverTcp = 11,

    /// <summary>
    /// FxSerialOverTcp
    /// </summary>
    [Description("FxSerialOverTcp")]
    FxSerialOverTcp = 12,

    /// <summary>
    /// IEC104
    /// </summary>
    [Description("IEC104")]
    IEC104 = 13,

    /// <summary>
    /// OpcUa
    /// </summary>
    [Description("OpcUa")]
    OpcUa = 14,

    #region Udp
    /// <summary>
    /// OmronFinsUdp
    /// </summary>
    [Description("OmronFinsUdp")]
    OmronFinsUdp = 15,
    #endregion

    #region 自由协议
    /// <summary>
    /// FJ1000Jet
    /// </summary>
    [Description("FJ1000Jet")]
    FJ1000Jet = 16,

    /// <summary>
    /// FJ60W
    /// </summary>
    [Description("FJ60W")]
    FJ60W = 17,

    /// <summary>
    /// GP1125T
    /// </summary>
    [Description("GP1125T")]
    GP1125T = 18,

    /// <summary>
    /// BottomImageF1970
    /// </summary>
    [Description("BottomImageF1970")]
    BottomImageF1970 = 19,
    #endregion

    #endregion

    #region COM
    /// <summary>
    /// ModbusRtu
    /// </summary>
    [Description("ModbusRtu")]
    ModbusRtu = 100,

    /// <summary>
    /// DLT6452007Serial
    /// </summary>
    [Description("DLT6452007Serial")]
    DLT6452007Serial = 101,

    /// <summary>
    /// CJT1882004Serial
    /// </summary>
    [Description("CJT1882004Serial")]
    CJT1882004Serial = 102,

    /// <summary>
    /// FxSerial
    /// </summary>
    [Description("FxSerial")]
    FxSerial = 103,
    #endregion

    #region API
    /// <summary>
    /// Api
    /// </summary>
    [Description("Api")]
    Api = 200,
    #endregion

    #region DATABASE
    /// <summary>
    /// MySQL
    /// </summary>
    [Description("MySQL")]
    MySQL = 300,
    #endregion
}