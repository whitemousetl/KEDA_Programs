using IoTBridge.Models.ProtocolParams;
using IoTBridge.Models.ProtocolResponses;
using IoTBridge.Services.Interfaces.Modbus;
using KEDA_Share.Enums;
using Newtonsoft.Json;
using Serilog;
using System;

namespace IoTBridge.Services.Implementations.Modbus;

public class ModbusRtuWebSocketHandler : IModbusRtuWebSocketHandler
{
    private readonly IModbusRtuService _modbusRtuService;
    private readonly IModbusRtuWriteNotifier _notifier;

    public ModbusRtuWebSocketHandler(IModbusRtuService modbusRtuService, IModbusRtuWriteNotifier notifier)
    {
        _modbusRtuService = modbusRtuService;
        _notifier = notifier;
    }
    public async Task<string> HandleRequestAsync(string jsonRequest)
    {
        if (string.IsNullOrWhiteSpace(jsonRequest))
            return JsonConvert.SerializeObject(new ModbusRtuResponse
            {
                ProtocolStatus = ProtocolStatus.AllDeviceFailture,
                ErrorMessage = "请求参数为空",
                DeviceResponses = []
            });

        ModbusRtuParams? modbusParams;
        try
        {
            modbusParams = JsonConvert.DeserializeObject<ModbusRtuParams>(jsonRequest);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "参数解析失败: ");
            return JsonConvert.SerializeObject(new ModbusRtuResponse
            {
                ProtocolStatus = ProtocolStatus.AllDeviceFailture,
                ErrorMessage = "参数解析失败: " + ex.Message,
                DeviceResponses = []
            });
        }


        // 写入队列逻辑，带异常处理
        bool hasWrite = false;
        if (modbusParams != null && modbusParams.Devices != null)
        {
            foreach (var device in modbusParams.Devices)
            {
                if (device.WriteMap != null && device.WriteMap.Length > 0)
                {
                    hasWrite = true;
                    foreach (var item in device.WriteMap)
                    {
                        try
                        {
                            _notifier.EnqueueWrite(item);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "写入队列异常: ");
                            return JsonConvert.SerializeObject(new ModbusRtuResponse
                            {
                                ProtocolStatus = ProtocolStatus.WriteListEnteredException,
                                ErrorMessage = "写入队列异常: " + ex.Message,
                                DeviceResponses = []
                            });
                        }
                    }
                }
            }
        }

        // 如果有写入操作，直接返回写入成功响应
        if (hasWrite)
        {
            return JsonConvert.SerializeObject(new ModbusRtuResponse
            {
                ProtocolStatus = ProtocolStatus.WriteListEntered,
                ErrorMessage = null,
                DeviceResponses = []
            });
        }

        try
        {
            var response = await _modbusRtuService.ReadAsync(modbusParams);
            return JsonConvert.SerializeObject(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "服务调用异常: ");
            return JsonConvert.SerializeObject(new ModbusRtuResponse
            {
                ProtocolStatus = ProtocolStatus.AllDeviceFailture,
                ErrorMessage = "服务调用异常: " + ex.Message,
                DeviceResponses = []
            });
        }
    }
}
