using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuQueue
{
    void EnqueueRead(List<ModbusReadPoint> points);
    void EnqueueWrite(List<ModbusWritePoint> point);
    bool TryDequeueWrite(out List<ModbusReadPoint>? points);
    bool TryDequeueRead(out List<ModbusWritePoint>? points);
    int WriteCount { get; }
    int ReadCount { get; }
}
