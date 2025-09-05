using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuConnectionManager
{
    (ModbusRtu? conn, string? message, bool isSuccess) GetConnection(ModbusRtuParams parameters);

    void CloseConnection();
}