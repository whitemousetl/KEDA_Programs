using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using KEDA_Share.Enums;
using IoTBridge.Models.ProtocolResponses;
using System.Net;
using System.Collections.Generic;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuService : IModbusRtuService
{
    private IModbusRtuConnectionManager _modbusRtuConnectionManager;
    private IModbusRtuDeviceReader _modbusRtuDeviceReader;

    public ModbusRtuService(IModbusRtuConnectionManager modbusRtuConnectionManager, IModbusRtuDeviceReader modbusRtuDeviceReader)
    {
        _modbusRtuConnectionManager = modbusRtuConnectionManager;
        _modbusRtuDeviceReader = modbusRtuDeviceReader;
    }

    public async Task<ModbusRtuResponse> ReadAsync(ModbusRtuParams modbusRtuParams)
    {
        var response = new ModbusRtuResponse();
        var modbusRtu = _modbusRtuConnectionManager.GetConnection(modbusRtuParams);

        foreach (var device in modbusRtuParams.Devices)
        {
            var result = await _modbusRtuDeviceReader.ReadDeviceAsync(device, modbusRtu);
            response.DeviceResponses.Add(result);
        }

        return response;
    }
}
