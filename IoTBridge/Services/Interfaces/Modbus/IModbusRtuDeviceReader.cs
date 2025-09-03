using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuDeviceReader
{
    Task<ModbusRtuDeviceResponse> ReadDeviceAsync(ModbusRtuDeviceParams device, ModbusRtu modbusRtu);
}
