using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations.Protocols
{
    /// <summary>
    /// 网口协议信息
    /// </summary>
    public class LanProtocolDto : ProtocolDto
    {
        /// <summary>
        /// 接口类型
        /// </summary>
        public override InterfaceType InterfaceType => InterfaceType.LAN;

        /// <summary>
        /// 代理网关
        /// </summary>
        public string Gateway { get; set; } = string.Empty;
    }
}
