using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuPointReader
{
    Task<ReadValueBase> ReadAsync(ModbusRtu modbusRtu, ReadMapItem point);
}