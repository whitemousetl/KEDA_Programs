using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuProviderFactory
{
    IModbusRtuProvider Create(SerialPortConfig config);
}