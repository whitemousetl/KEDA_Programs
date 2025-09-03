using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuConnectionManager
{
    ModbusRtu GetConnection(ModbusRtuParams parameters);
    void CloseConnection();
}
