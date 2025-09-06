using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusWriter
{
    Task ReadPointAsync(ModbusReadPoint point);
}
