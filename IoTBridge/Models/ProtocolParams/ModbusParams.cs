using HslCommunication.Core;
using KEDA_Share.Enums;
using System.IO.Ports;

namespace IoTBridge.Models.ProtocolParams;
/// <summary>
/// ModbusRtu协议参数
/// </summary>
/// <param name="Operation">操作  0:Read, 1:Write</param>
/// <param name="PortName">串口名称</param>
/// <param name="BaudRate">波特率</param>
/// <param name="DataBits">数据位</param>
/// <param name="StopBits">停止位  0:None, 1:One, 2:Two, 3:OnePointFive</param>
/// <param name="Parity">校验位  0:None, 1:Odd, 2:Even</param>
/// <param name="Devices">设备参数</param>
public record ModbusRtuParams(
    Operation Operation,
    string PortName,
    int BaudRate,
    byte DataBits,
    StopBits StopBits,
    Parity Parity,
    ModbusRtuDeviceParams[] Devices);

/// <summary>
/// ModbusRtu设备参数
/// </summary>
/// <param name="DeviceId">设备编号</param>
/// <param name="ReadMap">读取列表</param>
/// <param name="WriteMap">写入列表</param>
public record ModbusRtuDeviceParams(
    string DeviceId,
    ReadMapItem[]? ReadMap,
    WriteMapItem[]? WriteMap);

/// <summary>
/// 读取列表
/// </summary>
/// <param name="DataType">枚举类型  0:Bool, 1:Short, 2:UShort, 3:UInt, 4:Int, 5:Float, 6:Double, 7:String</param>
/// <param name="Address">地址</param>
/// <param name="Length">长度，字符串需要</param>
/// <param name="ReceiveTimeOut">接收超时</param>
/// <param name="SlaveAddress">从站地址</param>
/// <param name="ZeroBasedAddressing">地址是否从0开始？</param>
/// <param name="DataFormat">数据格式  0：ABCD, 1:BADC, 2:CDAB, 3:DCBA</param>
public record ReadMapItem(
    byte SlaveAddress,
    bool ZeroBasedAddressing,
    DataFormat DataFormat,
    ushort ReceiveTimeOut,
    DataType DataType,
    string Address,
    ushort? Length
);

/// <summary>
/// 写入列表
/// </summary>
/// <param name="Address">写入地址</param>
/// <param name="Value">写入值</param>
/// <param name="ReceiveTimeOut">接收超时</param>
/// <param name="SlaveAddress">从站地址</param>
/// <param name="ZeroBasedAddressing">地址是否从0开始？</param>
/// <param name="DataFormat">数据格式  0：ABCD, 1:BADC, 2:CDAB, 3:DCBA</param>
public record WriteMapItem(
    byte SlaveAddress,
    bool ZeroBasedAddressing,
    DataFormat DataFormat,
    ushort ReceiveTimeOut,
    DataType DataType,
    string Address,
    object? Value
    );