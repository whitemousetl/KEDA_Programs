using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;
using Serilog;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuConnectionManager : IModbusRtuConnectionManager
{
    private ModbusRtu _modbusRtu = null!;
    private ModbusRtuParams _currentParams = null!;
    public (ModbusRtu? conn, string? message, bool isSuccess) GetConnection(ModbusRtuParams modbusRtuParams)
    {
        bool isSuccess = false;
        string? msg = null;
        try
        {
            if (_modbusRtu == null || _currentParams == null || !modbusRtuParams.Equals(_currentParams) || !_modbusRtu.IsOpen())
            {
                _modbusRtu?.Close();
                _modbusRtu = new ModbusRtu();
                _modbusRtu.SerialPortInni(modbusRtuParams.PortName, modbusRtuParams.BaudRate, modbusRtuParams.DataBits, modbusRtuParams.StopBits, modbusRtuParams.Parity);
                var res = _modbusRtu.Open();
                isSuccess = res.IsSuccess;
                if (isSuccess) _currentParams = modbusRtuParams;
                else
                {
                    msg = res.Message;
                    Log.Error(msg, $"[串口] ModbusRtu打开串口时失败，串口:{modbusRtuParams.PortName}");
                }
            }
            else isSuccess = true;

            return (_modbusRtu, msg, isSuccess);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[串口] ModbusRtu打开串口时异常，串口:{modbusRtuParams.PortName}");
            msg = ex.Message;
            isSuccess = false;
            return (_modbusRtu, msg, isSuccess);
        }
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