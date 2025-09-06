using IoTBridge.Models.ProtocolParams;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusRtuScheduler
{
    Task ScheduleAsync<TPoint>(ModbusRtuConfig<TPoint> config);
}
