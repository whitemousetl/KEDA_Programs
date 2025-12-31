using HslCommunication.Core;
using KEDA_Common.Enums;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace KEDA_Common.Model;

public class WorkstationEntity//与mom对接的工作站配置，转换成json后存在WorkstationConfig表中
{
    public string EdgeID { get; set; } = string.Empty;
    public string EdgeName { get; set; } = string.Empty;
    public string Ip {  get; set; } = string.Empty;
    public List<ProtocolEntity> Protocols { get; set; } = [];
}
public class ProtocolEntity//协议实体，需要转换成json后存在ProtocolConfig表中
{
    public string ProtocolID { get; set; } = string.Empty;//mom上的协议编号,唯一标识
    public ProtocolInterface Interface { get; set; }//接口类型：LAN、COM
    public ProtocolType ProtocolType { get; set; }//协议类型
    public bool AddressStartWithZero { get; set; }//地址从0开始？
    public byte InstrumentType { get; set; }//仪表的类型，CJT188专用，
                                            //0x10：冷水水表；0x11：生活热水水表；0x12：直饮水水表；0x13：中水水表；
                                            //0x20：热量表（热量）；0x21：热量表（冷量）；
                                            //0x30：燃气表；0x40：电度表
    public string IPAddress { get; set; } = string.Empty;//ip地址
    public string Gateway { get; set; } = string.Empty;//网关
    public int ProtocolPort { get; set; }//端口
    public string PortName { get; set; } = string.Empty;//串口名称
    public int BaudRate { get; set; }//波特率
    public int DataBits { get; set; }//数据位
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; }//停止位
    public string Remark { get; set; } = string.Empty;//备注
    public int CollectCycle { get; set; }//采集周期，通讯延时
    public int ReceiveTimeOut { get; set; }//接收超时
    public int ConnectTimeOut { get; set; }//连接超时
    public List<DeviceEntity> Devices { get; set; } = [];
}

public class DeviceEntity
{
    public string EquipmentId {  get; set; } = string.Empty ;//设备Id
    public List<PointEntity> Points { get; set; } = [];
}

public class PointEntity
{
    public string Label {  get; set; } = string.Empty; //唯一标识，采集名称
    public string StationNo { get; set; } = string.Empty;//站号
    public DataType DataType { get; set; }//数据类型：bool，short，ushort，int，uint，float，double，string
    public string Address { get; set; } = string.Empty;//地址
    public ushort Length { get; set; }//读取长度
    public DataFormat Format { get; set; }//解析或生成格式，大端序小端序
    public string Change {  get; set; } = string.Empty;
}
