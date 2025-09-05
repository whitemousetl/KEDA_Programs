using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuCoordinator
{
    Task<ReadValueBase> ReadWithWritePrioritizeAsync(ModbusRtu modbusRtu, ReadMapItem point);
}
