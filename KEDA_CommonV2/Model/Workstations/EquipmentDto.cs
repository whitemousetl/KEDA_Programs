using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations
{
    /// <summary>
    /// 设备信息
    /// </summary>
    public class EquipmentDto
    {
        /// <summary>
        /// 设备Id
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 设备类型
        /// </summary>
        public EquipmentType EquipmentType { get; set; } = EquipmentType.Equipment;

        /// <summary>
        /// 变量信息列表
        /// </summary>
        public List<ParameterDto> Parameters { get; set; } = new List<ParameterDto>();
    }
}
