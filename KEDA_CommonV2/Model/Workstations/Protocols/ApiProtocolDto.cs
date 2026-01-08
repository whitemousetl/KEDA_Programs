using KEDA_CommonV2.Enums;
using System.Net;
using System.Text.Json;

namespace KEDA_CommonV2.Model.Workstations.Protocols;
/// <summary>
/// Api协议信息
/// </summary>
public class ApiProtocolDto : ProtocolDto
{
    /// <summary>,必须存在
    /// 接口类型
    /// </summary>
    public override InterfaceType InterfaceType => InterfaceType.API;

    /// <summary>
    /// 请求方式（默认Get）,必须存在
    /// </summary>
    public RequestMethod? RequestMethod { get; set; }

    /// <summary>
    /// 访问API语句,必须存在
    /// </summary>
    public string AccessApiString { get; set; } = string.Empty;

    /// <summary>
    /// 代理网关
    /// </summary>
    public string Gateway { get; set; } = string.Empty;
}
