using HslCommunication.ModBus;
using IoTBridge.Models.ProtocolParams;
using System.Threading.Channels;

namespace IoTBridge.Services.Interfaces.Modbus;

public interface IModbusQueue
{
    ChannelReader<ModbusReadPoint[]> ReadPointsReader { get; }
    ChannelReader<ModbusWritePoint[]> WritePointsReader { get; }
    void EnqueueRead(ModbusReadPoint[] readPoints); 
    void EnqueueWrite(ModbusWritePoint[] writePoints);
}
