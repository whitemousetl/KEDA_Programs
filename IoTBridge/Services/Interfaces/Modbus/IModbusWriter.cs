using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Implementations.Modbus;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusWriter
{
    Task WritePointAsync<T>(Func<string, T, Task<OperateResult>> writeFunc, ModbusWritePoint point);
}
