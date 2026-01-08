using KEDA_CommonV2.Enums;
using System.IO.Ports;
using System.Net;
using System.Text.Json;

namespace KEDA_CommonV2.Model.Workstations.Protocols;
/// <summary>
/// 串口协议信息
/// </summary>
public class SerialProtocolDto : ProtocolDto
{
    /// <summary>
    /// 接口类型,必须存在
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.COM;

    /// <summary>
    /// 串口名称,必须存在
    /// </summary>
    public string SerialPortName { get; set; } = string.Empty;

    /// <summary>
    /// 波特率,必须存在
    /// </summary>
    public BaudRateType? BaudRate { get; set; }

    /// <summary>
    /// 数据位,必须存在
    /// </summary>
    public DataBitsType? DataBits { get; set; }

    /// <summary>
    /// 校验位,必须存在
    /// </summary>
    public Parity? Parity { get; set; }

    /// <summary>
    /// 停止位,必须存在
    /// </summary>
    public StopBits? StopBits { get; set; }
}
