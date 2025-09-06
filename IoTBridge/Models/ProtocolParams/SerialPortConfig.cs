using System.IO.Ports;

namespace IoTBridge.Models.ProtocolParams;

/// <summary>
/// 通用串口参数
/// </summary>
/// <param name="PortName">串口名称</param>
/// <param name="BaudRate">波特率</param>
/// <param name="DataBits">数据位</param>
/// <param name="StopBits">停止位  0:None, 1:One, 2:Two, 3:OnePointFive</param>
/// <param name="Parity">校验位  0:None, 1:Odd, 2:Even</param>
public record SerialPortConfig(
    string PortName,
    int BaudRate,
    byte DataBits,
    StopBits StopBits,
    Parity Parity
);
