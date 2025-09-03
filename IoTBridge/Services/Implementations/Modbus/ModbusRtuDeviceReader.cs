using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuDeviceReader : IModbusRtuDeviceReader
{
    private readonly IModbusRtuPointReader _pointReader;

    public ModbusRtuDeviceReader(IModbusRtuPointReader pointReader)
    {
        _pointReader = pointReader;
    }

    //读设备时需要配置：1、从站地址  2、地址从0开始？ 3、数据格式
    public async Task<ModbusRtuDeviceResponse> ReadDeviceAsync(ModbusRtuDeviceParams device, ModbusRtu modbusRtu)
    {
        var result = new ModbusRtuDeviceResponse();
        result.DeviceId = device.DeviceId;

        modbusRtu.Station = device.SlaveAddress;
        modbusRtu.AddressStartWithZero = device.ZeroBasedAddressing;
        modbusRtu.DataFormat = device.DataFormat;

        foreach (var point in device.ReadMap)
        {
            var res = await _pointReader.ReadAsync(modbusRtu, point);
            result.Values.Add(res);
        }

        return result;
    }
}
