using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuPointWriter
{
    Task<(bool isSuccess, string? message)> WriteAsync(ModbusRtu modbusRtu, WriteMapItem writePoint);
}