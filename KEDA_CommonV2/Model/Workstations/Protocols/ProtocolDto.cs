using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations.Protocols
{
    /// <summary>
    /// 协议信息
    /// </summary>
    public abstract class ProtocolDto
    {
        /// <summary>
        /// 协议Id
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 接口类型
        /// </summary>
        public abstract InterfaceType InterfaceType { get; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType ProtocolType { get; set; }

        /// <summary>
        /// 通讯延时,默认0ms
        /// </summary>
        public int CollectCycle { get; set; }

        /// <summary>
        /// 接收超时,默认500ms
        /// </summary>
        public int ReceiveTimeOut { get; set; } = 500;

        /// <summary>
        /// 连接超时，默认500ms
        /// </summary>
        public int ConnectTimeOut { get; set; } = 500;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 设备信息列表
        /// </summary>
        public List<EquipmentDto> Equipments { get; set; } = new List<EquipmentDto>();
    }
}
