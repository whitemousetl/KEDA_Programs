using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations.Protocols
{
    /// <summary>
    /// Api协议信息
    /// </summary>
    public class ApiProtocolDto : ProtocolDto
    {
        /// <summary>
        /// 接口类型
        /// </summary>
        public override InterfaceType InterfaceType => InterfaceType.API;        

        /// <summary>
        /// 请求方式（默认Get）
        /// </summary>
        public RequestMethod RequestMethod { get; set; } = RequestMethod.Get;

        /// <summary>
        /// 访问API语句
        /// </summary>
        public string AccessApiString { get; set; } = string.Empty;
    }
}
