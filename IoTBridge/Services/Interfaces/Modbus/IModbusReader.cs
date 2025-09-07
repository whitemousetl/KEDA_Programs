using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusReader
{
    Task ReadPointAsync<T>(Func<string, ushort, Task<OperateResult<T[]>>> readFunc, ModbusReadPoint point);
    Task ReadPointAsync<T>(Func<string, ushort, Task<OperateResult<T>>> readFunc, ModbusReadPoint point);
}
