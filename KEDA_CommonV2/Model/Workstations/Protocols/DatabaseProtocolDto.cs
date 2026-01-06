using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations.Protocols
{
    /// <summary>
    /// 数据库协议信息
    /// </summary>
    public class DatabaseProtocolDto : ProtocolDto
    {
        /// <summary>
        /// 接口类型
        /// </summary>
        public override InterfaceType InterfaceType => InterfaceType.DATABASE;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int ProtocolPort { get; set; }

        /// <summary>
        /// 数据库名
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DatabaseConnectString { get; set; } = string.Empty;

        /// <summary>
        /// 查询SQL语句
        /// </summary>
        public string QuerySqlString { get; set; } = string.Empty;
    }
}
