using KEDA_CommonV2.Model.Workstations.Protocols;

namespace KEDA_CommonV2.Model.Workstations
{
    /// <summary>
    /// 边缘信息
    /// </summary>
    public class WorkstationDto
    {
        /// <summary>
        /// 边缘Id
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 边缘名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 协议信息 列表
        /// </summary>
        public List<ProtocolDto> Protocols { get; set; } = new List<ProtocolDto>();
    }
}
