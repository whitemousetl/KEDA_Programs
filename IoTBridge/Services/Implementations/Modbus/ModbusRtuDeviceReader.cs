using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using Serilog;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuDeviceReader : IModbusRtuDeviceReader
{
    private readonly IModbusRtuCoordinator _doordinator;

    public ModbusRtuDeviceReader(IModbusRtuCoordinator doordinator)
    {
        _doordinator = doordinator;
    }

    public async Task<ModbusRtuDeviceResponse> ReadDeviceAsync(ModbusRtuDeviceParams device, ModbusRtu modbusRtu)
    {
        try
        {
            var result = new ModbusRtuDeviceResponse();
            result.DeviceId = device.DeviceId;

            if(device.ReadMap == null || device.ReadMap.Length == 0)
            {
                result.IsSuccess = false;
                result.IsOnline = false;
                result.Message = "设备的读取参数列表为空";
                return result;
            }

            foreach (var point in device.ReadMap)
            {
                var res = await _doordinator.ReadWithWritePrioritizeAsync(modbusRtu, point);
                result.Values.Add(res);
            }

            result.IsSuccess = result.Values.All(x => x.IsSuccess);
            result.IsOnline = result.Values.Any(x => x.IsSuccess);

            if (!result.IsOnline) result.Message = "设备异常或离线，请检查";
            if (result.IsOnline && !result.IsSuccess) result.Message = "设备采集点部分异常，请检查";

            if(!string.IsNullOrWhiteSpace(result.Message))
            Log.Error(result.Message, $"[设备] ModbusRtu读取设备时发生异常，设备Id:{device.DeviceId}");

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[设备] ModbusRtu读取设备时发生异常，设备Id:{device.DeviceId}");
            return new ModbusRtuDeviceResponse 
            { 
                DeviceId = device.DeviceId,
                IsSuccess = false,
                IsOnline = false,
                Message = $"[设备] ModbusRtu读取设备时发生异常，设备Id:{device.DeviceId}" + ex.Message,
            };
        }
    }
}
