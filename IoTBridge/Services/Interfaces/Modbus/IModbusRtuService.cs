using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuService
{
    Task<ModbusRtuResponse> ReadAsync(ModbusRtuParams? modbusRtuParams);
}