using HslCommunication.Core;
using KEDA_CommonV2.Enums;
using System.IO.Ports;

namespace KEDA_CommonV2.Model;

public class Workstation//与mom对接的工作站配置，转换成json后存在WorkstationConfig表中
{
    public string WorkstationId { get; set; } = string.Empty;
    public string WorkstationName { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public List<Protocol> Protocols { get; set; } = [];
}

public abstract class Protocol
{
    public string ProtocolId { get; set; } = string.Empty;
    public abstract InterfaceType InterfaceType { get; }
    public ProtocolType ProtocolType { get; set; }
    public string Remark { get; set; } = string.Empty;
    public int CollectCycle { get; set; }
    public int ReceiveTimeOut { get; set; }
    public int ConnectTimeOut { get; set; }
    public List<Equipment> Equipments { get; set; } = [];
}

// 网口协议
public class LanProtocol : Protocol
{
    public override InterfaceType InterfaceType => InterfaceType.LAN;
    public string IpAddress { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public int ProtocolPort { get; set; }
}

// 串口协议
public class SerialProtocol : Protocol
{
    public override InterfaceType InterfaceType => InterfaceType.COM;
    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int DataBits { get; set; }
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; }
}

// WebAPI协议
public class ApiProtocol : Protocol
{
    //核心属性
    public override InterfaceType InterfaceType => InterfaceType.API;
    public string OueryApiString { get; set; } = string.Empty; // 接口地址
    public RequestMethod RequestMethod { get; set; } = RequestMethod.Get; // GET, POST, PUT, DELETE等

    //扩展属性
    public string BaseUrl { get; set; } = string.Empty; // API基础URL

    public string Endpoint { get; set; } = string.Empty; // API端点路径
    public AuthenticationType AuthType { get; set; } = AuthenticationType.None;  // 认证方式
    public string ApiKey { get; set; } = string.Empty; // API密钥
    public string Token { get; set; } = string.Empty; // 访问令牌
    public string BearerToken { get; set; } = string.Empty; // Bearer Token
    public string RequestBody { get; set; } = string.Empty; // 请求体模板
    public ContentType ContentType { get; set; } = ContentType.Json; // application/json, application/xml等
    public string CertificatePath { get; set; } = string.Empty; // 证书路径（HTTPS）
    public bool IgnoreSslErrors { get; set; } // 是否忽略SSL错误
    public int RetryCount { get; set; } = 3; // 重试次数
    public int RetryInterval { get; set; } = 1000; // 重试间隔（毫秒）
    public Dictionary<string, string> Headers { get; set; } = []; // 自定义请求头
}

// 数据库协议
public class DatabaseProtocol : Protocol
{
    //核心属性
    public override InterfaceType InterfaceType => InterfaceType.DATABASE;

    public string ConnectionString { get; set; } = string.Empty;
    public string OuerySqlString { get; set; } = string.Empty;//SqlQuery

    //扩展属性
    public string IpAddress { get; set; } = string.Empty;
    public int ProtocolPort { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseAccount { get; set; } = string.Empty;
    public string DatabasePassword { get; set; } = string.Empty;
    public string AdditionalOptions { get; set; } = string.Empty; // 其他参数（可选）
}

public class Equipment
{
    public string EquipmentId { get; set; } = string.Empty;//设备Id
    public string EquipmentName { get; set; } = string.Empty; //设备名称
    public EquipmentType EquipmentType { get; set; } = 0; // 设备类型 设备/仪表
    public List<Point> Points { get; set; } = [];
}

public class Point
{
    public string Label { get; set; } = string.Empty; //唯一标识，采集名称
    public string StationNo { get; set; } = string.Empty;//站号
    public DataType DataType { get; set; }//数据类型：bool，short，ushort，int，uint，float，double，string
    public string Address { get; set; } = string.Empty;//地址, 虚拟点固定地址VirtualPoint
    public ushort Length { get; set; }//读取长度
    public string Default { get; set; } = string.Empty; // 默认值
    public int Cycle { get; set; } //采集周期
    public string Change { get; set; } = string.Empty; //表达式，一元一次方程，进制转换，虚拟点计算
    public string MinValue { get; set; } = string.Empty; // 最小值
    public string MaxValue { get; set; } = string.Empty; // 最大值
    public DataFormat Format { get; set; }//解析或生成格式，大端序小端序
    public bool AddressStartWithZero { get; set; }//地址从0开始？
    public InstrumentType InstrumentType { get; set; }//仪表的类型，CJT188专用，

    //0x10：冷水水表；0x11：生活热水水表；0x12：直饮水水表；0x13：中水水表；
    //0x20：热量表（热量）；0x21：热量表（冷量）；
    //0x30：燃气表；0x40：电度表
    public string Value { get; set; } = string.Empty;
}