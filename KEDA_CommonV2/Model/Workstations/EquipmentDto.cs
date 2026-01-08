using KEDA_CommonV2.Enums;
using System.Net;
using System.Text.Json;

namespace KEDA_CommonV2.Model.Workstations;
/// <summary>
/// 设备信息
/// </summary>
public class EquipmentDto
{
    /// <summary>
    /// 设备Id，必须存在
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称，非必须存在
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型，必须存在
    /// </summary>
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

    /// <summary>
    /// 变量信息列表，必须存在
    /// </summary>
    public List<ParameterDto> Parameters { get; set; } = [];
}
