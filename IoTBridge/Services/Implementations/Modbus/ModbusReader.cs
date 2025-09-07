using HslCommunication;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusReader : IModbusReader
{
    public async Task ReadPointAsync<T>(Func<string, ushort, Task<OperateResult<T[]>>> readFunc, ModbusReadPoint point)
    {
        if(point.Length.HasValue) 
            await readFunc.Invoke(point.Address, point.Length.Value);
        else
            await readFunc.Invoke(point.Address, 1);
    }

    public async Task ReadPointAsync<T>(Func<string, ushort, Task<OperateResult<T>>> readFunc, ModbusReadPoint point)
    {
        if (point.Length.HasValue)
            await readFunc.Invoke(point.Address, point.Length.Value);
        else
            await readFunc.Invoke(point.Address, 1);
    }
}
