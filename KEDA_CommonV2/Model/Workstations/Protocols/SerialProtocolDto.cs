using KEDA_CommonV2.Enums;
using System.IO.Ports;

namespace KEDA_CommonV2.Model.Workstations.Protocols
{
    /// <summary>
    /// 串口协议信息
    /// </summary>
    public class SerialProtocolDto : ProtocolDto
    {
        /// <summary>
        /// 接口类型
        /// </summary>
        public override InterfaceType InterfaceType => InterfaceType.COM;

        /// <summary>
        /// 波特率
        /// </summary>
        public BaudRateType BaudRate { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public DataBitsType DataBits { get; set; }

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; }
    }
}
