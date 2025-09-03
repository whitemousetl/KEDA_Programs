using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuConnectionManager : IModbusRtuConnectionManager
{
    private ModbusRtu _modbusRtu = null!;
    private ModbusRtuParams _currentParams = null!;
    public ModbusRtu GetConnection(ModbusRtuParams modbusRtuParams)
    {
        if (_modbusRtu == null || _currentParams == null || !modbusRtuParams.Equals(_currentParams) || !_modbusRtu.IsOpen())
        {
            _modbusRtu?.Close();
            _modbusRtu = new ModbusRtu();
            _modbusRtu.SerialPortInni(modbusRtuParams.PortName, modbusRtuParams.BaudRate, modbusRtuParams.DataBits, modbusRtuParams.StopBits, modbusRtuParams.Parity);
            _modbusRtu.Open();
            _modbusRtu.ReceiveTimeOut = modbusRtuParams.ReceiveTimeOut;
            _currentParams = modbusRtuParams;
        }

        return _modbusRtu;
    }

    public void CloseConnection()
    {
        if (_modbusRtu != null && _modbusRtu.IsOpen())
        {
            _modbusRtu.Close();
        }
        _modbusRtu = null!;
        _currentParams = null!;
    }
}
