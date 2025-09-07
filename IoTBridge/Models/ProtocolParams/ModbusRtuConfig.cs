using HslCommunication.Core;
using KEDA_Share.Enums;
using System.IO.Ports;

namespace IoTBridge.Models.ProtocolParams;
/// <summary>
/// ModbusRtu读取配置
/// </summary>
/// <param name="PortConfig">通用串口参数</param>
/// <param name="Devices">设备参数</param>
public record ModbusRtuConfig(Operation Operation, SerialPortConfig PortConfig, ModbusDevice[] Devices);

/// <summary>
/// ModbusRtu设备
/// </summary>
/// <param name="DeviceId">设备编号</param>
/// <param name="ReadMap">读取列表</param>
public record ModbusDevice(string DeviceId, ModbusReadPoint[]? ReadPoints, ModbusWritePoint[]? WritePoints);

/// <summary>
/// Modbus读取点位
/// </summary>
/// <param name="DataType">枚举类型  0:Bool, 1:Short, 2:UShort, 3:UInt, 4:Int, 5:Float, 6:Double, 7:String</param>
/// <param name="Address">地址</param>
/// <param name="Length">长度，字符串需要</param>
/// <param name="ReceiveTimeOut">接收超时</param>
/// <param name="SlaveAddress">从站地址</param>
/// <param name="ZeroBasedAddressing">地址是否从0开始？</param>
/// <param name="DataFormat">数据格式  0：ABCD, 1:BADC, 2:CDAB, 3:DCBA</param>
public record ModbusReadPoint(
    byte SlaveAddress,
    bool ZeroBasedAddressing,
    DataFormat DataFormat,
    ushort ReceiveTimeOut,
    DataType DataType,
    string Address,
    ushort? Length
);

/// <summary>
/// Modbus写入点位
/// </summary>
/// <param name="DataType">枚举类型  0:Bool, 1:Short, 2:UShort, 3:UInt, 4:Int, 5:Float, 6:Double, 7:String</param>
/// <param name="Address">地址</param>
/// <param name="ReceiveTimeOut">接收超时</param>
/// <param name="SlaveAddress">从站地址</param>
/// <param name="ZeroBasedAddressing">地址是否从0开始？</param>
/// <param name="DataFormat">数据格式  0：ABCD, 1:BADC, 2:CDAB, 3:DCBA</param>
/// <param name="Value">需要写入的值</param>
public record ModbusWritePoint(
    byte SlaveAddress,
    bool ZeroBasedAddressing,
    DataFormat DataFormat,
    ushort ReceiveTimeOut,
    DataType DataType,
    string Address,
    object Value
);