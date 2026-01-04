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
        /// 数据库连接配置类型
        /// </summary>
        public ConnectStringConfigType ConnectStringConfigType { get; set; }

        /// <summary>
        /// 数据库账号
        /// </summary>
        public string DatabaseAccount { get; set; } = string.Empty;

        /// <summary>
        /// 数据库密码
        /// </summary>
        public string DatabasePassword { get; set; } = string.Empty;

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
