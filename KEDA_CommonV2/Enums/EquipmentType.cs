using System.ComponentModel;

namespace KEDA_CommonV2.Enums;

/// <summary>
/// 仪器类型
/// </summary>
public enum EquipmentType
{
    /// <summary>
    /// 设备
    /// </summary>
    [Description("设备")]
    Equipment = 0,

    /// <summary>
    /// 仪表
    /// </summary>
    [Description("仪表")]
    Instrument = 1
}