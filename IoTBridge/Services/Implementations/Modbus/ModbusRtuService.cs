using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;

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

    public async Task<ModbusRtuResponse> ReadAsync(ModbusRtuParams? modbusRtuParams)
    {
        var response = new ModbusRtuResponse();

        // 参数校验
        if (modbusRtuParams == null || modbusRtuParams.Devices == null || modbusRtuParams.Devices.Length == 0)
        {
            response.ProtocolStatus = ProtocolStatus.AllDeviceFailture;
            response.ErrorMessage = "ModbusRtu参数或设备列表为空";
            response.DeviceResponses = [];
            return response;
        }

        try
        {
            var (conn, message, isSuccess) = _modbusRtuConnectionManager.GetConnection(modbusRtuParams);

            if (!isSuccess || conn == null)
            {
                response.DeviceResponses = modbusRtuParams.Devices
                    .Select(dev => new ModbusRtuDeviceResponse
                    {
                        DeviceId = dev.DeviceId,
                        IsSuccess = false,
                        IsOnline = false,
                        Message = $"ModbusRtu打开串口失败或返回null，请检查：{message}"
                    })
                    .ToList();
                response.ProtocolStatus = ProtocolStatus.AllDeviceFailture;
                response.ErrorMessage = message;
                return response;
            }

            foreach (var device in modbusRtuParams.Devices)
            {
                var result = await _modbusRtuDeviceReader.ReadDeviceAsync(device, conn);
                response.DeviceResponses.Add(result);
            }

            // 状态判断
            if (response.DeviceResponses.All(d => d.IsSuccess))
                response.ProtocolStatus = ProtocolStatus.AllDeviceSuccess;
            else if (response.DeviceResponses.All(d => !d.IsSuccess))
                response.ProtocolStatus = ProtocolStatus.AllDeviceFailture;
            else
                response.ProtocolStatus = ProtocolStatus.PartialDeviceSuccess;
        }
        catch (Exception ex)
        {
            response.DeviceResponses = modbusRtuParams.Devices
                   .Select(dev => new ModbusRtuDeviceResponse
                   {
                       DeviceId = dev.DeviceId,
                       IsSuccess = false,
                       IsOnline = false,
                       Message = "ModbusRtu打开串口时发生异常，请检查" + ex.Message
                   })
                   .ToList();
            response.ProtocolStatus = ProtocolStatus.AllDeviceFailture;
            response.ErrorMessage = ex.Message;
        }

        return response;
    }
}