using HslCommunication;
using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Implementations.Modbus;

using IoTBridge.Services.Interfaces.Modbus;
using System.Threading.Tasks;
public class ModbusRtuProvider : IModbusRtuProvider
{
    private ModbusRtu _modbusRtu;
    private SerialPortConfig? _lastConfig;
    private ModbusRtu? _lastModbusRtu;

    public bool IsOpen => _modbusRtu.IsOpen();


    public ModbusRtuProvider(SerialPortConfig config)
    {
        _modbusRtu = CreateOrReturnModbusRtu(config);
    }

    private ModbusRtu CreateOrReturnModbusRtu(SerialPortConfig config)
    {
        // 判断config是否与上次相同
        if (_lastConfig != null && _lastConfig == config && _lastModbusRtu != null)
        {
            if(!_lastModbusRtu.IsOpen())
                _lastModbusRtu.Open();
            return _lastModbusRtu;
        }

        var modbus = new ModbusRtu();
        modbus.SerialPortInni(config.PortName, config.BaudRate, config.DataBits, config.StopBits, config.Parity);

        modbus.Open();

        _lastConfig = config;
        _lastModbusRtu = modbus;
        return modbus;
    }

    // 检查串口状态后打开
    public void Open()
    {
        if (!_modbusRtu.IsOpen())
            _modbusRtu.Open();
    }

    // 检查串口状态后关闭
    public void Close()
    {
        if (_modbusRtu.IsOpen())
            _modbusRtu.Close();
    }

    public void Reset()
    {
        if (_modbusRtu.IsOpen())
        {
            _modbusRtu.Close();
            _modbusRtu.Open();
        }
    }

    // 读方法
    public Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length) => _modbusRtu.ReadBoolAsync(address, length);
    public Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length) => _modbusRtu.ReadInt16Async(address, length);
    public Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length) => _modbusRtu.ReadUInt16Async(address, length);
    public Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length) => _modbusRtu.ReadInt32Async(address, length);
    public Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length) => _modbusRtu.ReadUInt32Async(address, length);
    public Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length) => _modbusRtu.ReadFloatAsync(address, length);
    public Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length) => _modbusRtu.ReadDoubleAsync(address, length);
    public Task<OperateResult<string>> ReadStringAsync(string address, ushort length) => _modbusRtu.ReadStringAsync(address, length);

    // 写方法
    public Task<OperateResult> WriteBoolAsync(string address, bool value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteInt16Async(string address, short value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteUInt16Async(string address, ushort value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteInt32Async(string address, int value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteUInt32Async(string address, uint value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteFloatAsync(string address, float value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteDoubleAsync(string address, double value) => _modbusRtu.WriteAsync(address, value);
    public Task<OperateResult> WriteStringAsync(string address, string value) => _modbusRtu.WriteAsync(address, value);

    public void Dispose() => _modbusRtu?.Dispose();
}
