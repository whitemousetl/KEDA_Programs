using HslCommunication.Core;
using KEDA_CommonV2.Enums;

namespace KEDA_CommonV2.Model.Workstations
{
    /// <summary>
    /// 变量信息
    /// </summary>
    public class ParameterDto
    {
        /// <summary>
        /// 参数Id
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 参数名
        /// </summary>
        public string Label { get; set; } = string.Empty; 

        /// <summary>
        /// 站号
        /// </summary>
        public string StationNo { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 地址, 虚拟点固定地址VirtualPoint
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 长度
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 采集周期
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// 正表达式，一元一次方程，进制转换，虚拟点计算
        /// </summary>
        public string PositiveExpression { get; set; } = string.Empty;

        /// <summary>
        /// 最小值
        /// </summary>
        public string MinValue { get; set; } = string.Empty;

        /// <summary>
        /// 最大值
        /// </summary>
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>
        /// 协议格式,解析或生成格式，大端序小端序
        /// </summary>
        public DataFormat DataFormat { get; set; }

        /// <summary>
        /// 偏移量，地址从0开始？
        /// </summary>
        public bool AddressStartWithZero { get; set; }

        /// <summary>
        /// 仪表类型，CJT188专用
        /// </summary>
        public MeterType InstrumentType { get; set; }

        /// <summary>
        /// 写入才有值
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
