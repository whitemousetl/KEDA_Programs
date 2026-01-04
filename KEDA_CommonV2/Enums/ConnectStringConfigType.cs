using System.ComponentModel;

namespace KEDA_CommonV2.Enums
{
    /// <summary>
    /// 数据库连接配置类型
    /// </summary>
    public enum ConnectStringConfigType
    {
        /// <summary>
        /// 空
        /// </summary>
        [Description("空")]
        None = 0,

        /// <summary>
        /// 分开
        /// </summary>
        [Description("分开")]
        Separate = 1,

        /// <summary>
        /// 合并
        /// </summary>
        [Description("合并")]
        Merge = 2,
    }
}
