using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using KEDA_Share.Enums;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuPointReader
{
    Task<ReadValueBase> ReadAsync(ModbusRtu modbusRtu, ReadMapItem point);
}
