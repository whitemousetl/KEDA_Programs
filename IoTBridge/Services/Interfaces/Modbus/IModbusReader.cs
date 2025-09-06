using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusReader
{
    Task ReadPointAsync(ModbusWritePoint point);
}
