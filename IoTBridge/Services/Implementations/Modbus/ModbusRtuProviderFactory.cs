using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuProviderFactory : IModbusRtuProviderFactory
{
    public IModbusRtuProvider Create(SerialPortConfig config) => new ModbusRtuProvider(config);
}
