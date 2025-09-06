using IoTBridge.Models.ProtocolParams;
using IoTBridge.Services.Interfaces.Modbus;

namespace IoTBridge.Services.Implementations;

//public class ModbusRtuScheduler : IModbusRtuScheduler
//{
//    private readonly IModbusRtuScheduler _scheduler;

//    public ModbusRtuScheduler(IModbusRtuScheduler scheduler)
//    {
//        _scheduler = scheduler;
//    }

//    public Task ScheduleAsync<TPoint>(ModbusRtuConfig<TPoint> config)
//    {
//        if(typeof(TPoint) == typeof(ModbusReadPoint))
//        {

//        }
//        else if(typeof(TPoint) == typeof(ModbusWritePoint))
//        {

//        }
//        else
//        {
//            throw new NotSupportedException($"不支持的点类型: {typeof(TPoint).Name}");
//        }
//    }
//}
