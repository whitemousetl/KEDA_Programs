using HslCommunication;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusWriter : IModbusWriter
{
    public async Task WritePointAsync<T>(Func<string, T, Task<OperateResult>> writeFunc, ModbusWritePoint point)
    {
        await writeFunc.Invoke(point.Address, (T)point.Value);
    }
}
