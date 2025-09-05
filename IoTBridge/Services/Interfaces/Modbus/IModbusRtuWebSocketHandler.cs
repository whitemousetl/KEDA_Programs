namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuWebSocketHandler
{
    Task<string> HandleRequestAsync(string jsonRequest);
}